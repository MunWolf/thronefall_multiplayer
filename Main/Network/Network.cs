using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Steamworks;
using ThronefallMP.Components;
using ThronefallMP.NetworkPackets;
using ThronefallMP.NetworkPackets.Game;
using ThronefallMP.Patches;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThronefallMP.Network;

public class Network : MonoBehaviour
{
    // TODO: Use this instead of int.
    public enum Channel
    {
        General,
        NetworkManagement
    }

    public delegate void ChatMessage(string user, string message);
    public event ChatMessage OnReceivedChatMessage;
    
    private const int MaxMessages = 20;
    
    private Callback<LobbyChatUpdate_t> _lobbyChatUpdate;
    private Callback<GameLobbyJoinRequested_t> _lobbyJoinRequested;
    private Callback<LobbyChatMsg_t> _lobbyChatMessage;
    
    private Callback<SteamNetworkingMessagesSessionRequest_t> _sessionRequestCallback;
    private Callback<SteamNetworkingMessagesSessionFailed_t> _sessionFailed;
    
    private CallResult<LobbyEnter_t> _lobbyEnterResult;

    private readonly PlayerNetworkData.Shared _latestLocalData = new PlayerNetworkData.Shared();
    private readonly List<CSteamID> _peers = new();
    private readonly Dictionary<CSteamID, PlayerManager.Player> _players = new();
    private readonly IntPtr[] _messages = new IntPtr[MaxMessages];

    public int MaxPlayers { get; set; }
    public bool Authority => Server || !Online;
    public bool Server { get; private set; }
    public bool Online { get; private set; }

    private CSteamID _lobby;
    private CSteamID _owner;
    private string _password;
    private byte[] _chatBuffer = new byte[4096];
    
    public void Awake()
    {
        _sessionRequestCallback = Callback<SteamNetworkingMessagesSessionRequest_t>.Create(OnSessionRequest);
        _sessionFailed = Callback<SteamNetworkingMessagesSessionFailed_t>.Create(OnSessionFailed);
        _lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
        _lobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        _lobbyEnterResult = CallResult<LobbyEnter_t>.Create(OnLobbyEntered);
        _lobbyChatMessage = Callback<LobbyChatMsg_t>.Create(OnChatMessageReceived);
        SceneManager.sceneLoaded += OnSceneChanged;
    }

    private void OnChatMessageReceived(LobbyChatMsg_t chat)
    {
        var type = (EChatEntryType)chat.m_eChatEntryType;
        if (type != EChatEntryType.k_EChatEntryTypeChatMsg)
        {
            return;
        }

        var length = SteamMatchmaking.GetLobbyChatEntry(
            new CSteamID(chat.m_ulSteamIDLobby),
            (int)chat.m_iChatID,
            out var user,
            _chatBuffer,
            _chatBuffer.Length,
            out type
        );

        var username = SteamFriends.GetFriendPersonaName(user);
        var message = Encoding.ASCII.GetString(_chatBuffer, 0, length);
        OnReceivedChatMessage?.Invoke(username, message);
    }
    
    public void SendChatMessage(string message)
    {
        if (!_lobby.IsValid())
        {
            return;
        }

        var output = Encoding.ASCII.GetBytes(message);
        SteamMatchmaking.SendLobbyChatMsg(_lobby, output, output.Length);
    }
    
    public bool Authenticate(string password)
    {
        return string.IsNullOrEmpty(_password) || _password == password;
    }
    
    public void AddPlayer(CSteamID id)
    {
        var player = Plugin.Instance.PlayerManager.Create(Plugin.Instance.PlayerManager.GenerateID());
        _players[id] = player;
        var packet = new PeerSyncPacket();
        Plugin.Log.LogInfo($"Building peer sync");
        
        foreach (var data in Plugin.Instance.PlayerManager.GetAllPlayerData())
        {
            Plugin.Log.LogInfo($" {data.id} -> {data.SharedData.Position}");
            packet.Players.Add(new PeerSyncPacket.PlayerData
            {
                Id = data.id,
                Position = data.SharedData.Position
            });
        }
        
        foreach (var pair in _players)
        {
            Plugin.Log.LogInfo($"Sending peer sync to {pair.Key.m_SteamID}");
            packet.LocalPlayer = pair.Value.Id;
            var sid = new SteamNetworkingIdentity();
            sid.SetSteamID(pair.Key);
            SendSingle(packet, sid);
        }
    }

    public void KickPeer(CSteamID id, DisconnectPacket.Reason reason)
    {
        if (_players.ContainsKey(id))
        {
            Plugin.Log.LogInfo($"Player {id.m_SteamID} kicked with reason {reason}");
            Plugin.Instance.PlayerManager.Remove(_players[id].Id);
            _players.Remove(id);
        }
        
        _peers.Remove(id);
        var sid = new SteamNetworkingIdentity();
        sid.SetSteamID(id);
        var packet = new DisconnectPacket()
        {
            DisconnectReason = reason
        };
        SendSingle(packet, sid);
        SteamNetworkingMessages.CloseSessionWithUser(ref sid);
    }
    
    private void OnSessionFailed(SteamNetworkingMessagesSessionFailed_t failed)
    {
        _peers.Remove(failed.m_info.m_identityRemote.GetSteamID());
        if (!Server)
        {
            // TODO: Show an error
            Online = _peers.Count != 0;
            Server = true;
        }
        else
        {
            // TODO: Show notification that client left.
        }
    }

    private void Update()
    {
        if (_peers.Count > 0)
        {
            foreach (var channel in Enum.GetValues(typeof(Channel)))
            {
                var received = SteamNetworkingMessages.ReceiveMessagesOnChannel(
                    (int)channel,
                    _messages,
                    MaxMessages
                );

                for (var i = 0; i < received; ++i)
                {
                    var message = Marshal.PtrToStructure<SteamNetworkingMessage_t>(_messages[i]);
                    var buffer = new Buffer(message.m_cbSize);
                    Marshal.Copy(message.m_pData, buffer.Data, 0, buffer.Data.Length);
                    HandlePacket(ref message.m_identityPeer, buffer);
                    SteamNetworkingMessage_t.Release(_messages[i]);
                }
            }
        }
        
        var shared = Plugin.Instance.PlayerManager.LocalPlayer?.Shared;
        if (shared == null || shared == _latestLocalData)
        {
            return;
        }
        
        var packet = new PlayerSyncPacket
        {
            PlayerID = Plugin.Instance.PlayerManager.LocalId,
            Data = shared
        };
        Send(packet);
        _latestLocalData.Set(shared);
    }

    private void CloseLobby()
    {
        if (_lobby.IsValid())
        {
            Plugin.Log.LogInfo($"Leaving lobby {_lobby.m_SteamID}");
            SteamMatchmaking.LeaveLobby(_lobby);
            foreach (var peer in _peers)
            {
                var sid = new SteamNetworkingIdentity();
                sid.SetSteamID(peer);
                SteamNetworkingMessages.CloseSessionWithUser(ref sid);
            }
            
            _peers.Clear();
            _lobby.Clear();
        }
    }
    
    public void Local()
    {
        Plugin.Log.LogInfo("Switched to Local");
        _password = null;
        Server = true;
        Online = false;
        
        CloseLobby();
        
        Plugin.Instance.PlayerManager.Clear();
        Plugin.Instance.PlayerManager.LocalId = Plugin.Instance.PlayerManager.GenerateID();
        Plugin.Instance.PlayerManager.Create(Plugin.Instance.PlayerManager.LocalId);
    }

    public void Host(CSteamID lobby, string password)
    {
        CloseLobby();
        
        Plugin.Log.LogInfo($"Switched to Host in lobby {lobby.m_SteamID} server {SteamUser.GetSteamID()}");
        _password = password;
        _lobby = lobby;
        _owner = SteamUser.GetSteamID();
        Server = true;
        Online = true;
        Plugin.Instance.PlayerManager.Clear();
        Plugin.Instance.PlayerManager.LocalId = Plugin.Instance.PlayerManager.GenerateID();
        Plugin.Instance.PlayerManager.Create(Plugin.Instance.PlayerManager.LocalId);
        SteamMatchmaking.SetLobbyJoinable(lobby, true);
    }

    public void ConnectLobby(CSteamID lobby)
    {
        CloseLobby();
        _lobbyEnterResult.Set(SteamMatchmaking.JoinLobby(lobby));
    }

    private void Connect(CSteamID lobby, string password)
    {
        Plugin.Log.LogInfo("Switched to Connect");
        _password = password;
        _lobby = lobby;
        Server = false;
        Online = true;
        Plugin.Instance.PlayerManager.Clear();
        _owner = SteamMatchmaking.GetLobbyOwner(lobby);
        
        Plugin.Log.LogInfo($"Sending approval packet to server {_owner.m_SteamID}");
        var approvalPacket = new ApprovalPacket()
        {
            Password = password
        };

        _peers.Add(_owner);
        var id = new SteamNetworkingIdentity();
        id.SetSteamID(_owner);
        SendSingle(approvalPacket, id);
    }

    private void OnSceneChanged(Scene scene, LoadSceneMode mode)
    {
        Plugin.Log.LogInfo($"Scene loaded '{scene.name}'");
        if (scene.name == "_StartMenu")
        {
            Plugin.Instance.PlayerManager.SetPrefab(null);
            Local();
            return;
        }
        
        if (!Server || !Online || !_lobby.IsValid())
        {
            return;
        }

        Plugin.Log.LogInfo($"Can join lobby: {SceneManager.GetSceneByName("_LevelSelect").isLoaded}");
        SteamMatchmaking.SetLobbyJoinable(_lobby, SceneManager.GetSceneByName("_LevelSelect").isLoaded);
    }

    public void Send(IPacket packet, bool handleLocal = false, SteamNetworkingIdentity except = new())
    {
        if (Online)
        {
            var buffer = new Buffer();
            buffer.Write((int)packet.TypeID);
            packet.Send(buffer);
            var sid = new SteamNetworkingIdentity();
            foreach (var peer in _peers)
            {
                sid.SetSteamID(peer);
                Send(buffer, sid, packet.DeliveryMask, packet.Channel);
            }
        }

        if (handleLocal)
        {
            var id = new SteamNetworkingIdentity();
            if (SteamManager.Initialized)
            {
                id.SetSteamID(SteamUser.GetSteamID());
            }
            
            PacketHandler.HandlePacket(id, packet);
        }
    }

    public void SendSingle(IPacket packet, SteamNetworkingIdentity target)
    {
        var buffer = new Buffer();
        buffer.Write((int)packet.TypeID);
        packet.Send(buffer);
        Send(buffer, target, packet.DeliveryMask, packet.Channel);
    }

    private static void Send(Buffer buffer, SteamNetworkingIdentity target, int flags, int channel)
    {
        if (target.IsInvalid())
        {
            Plugin.Log.LogInfo($"Trying to send with an invalid id '{target.GetSteamID().m_SteamID}'");
            Plugin.Log.LogInfo(Environment.StackTrace);
            return;
        }

        var handle = GCHandle.Alloc(buffer.Data, GCHandleType.Pinned);
        var pointer = handle.AddrOfPinnedObject();
        
        SteamNetworkingMessages.SendMessageToUser(
            ref target,
            pointer,
            (uint)buffer.WriteHead,
            flags,
            channel
        );
        handle.Free();
    }
    
    private void HandlePacket(ref SteamNetworkingIdentity sender, Buffer buffer)
    {
        var type = (PacketId)buffer.ReadInt32();
        var shouldPropagate = false;
        IPacket packet = null;
        switch (type)
        {
            case PlayerSyncPacket.PacketID:
            {
                packet = new PlayerSyncPacket();
                shouldPropagate = Server;
                break;
            }
            case TransitionToScenePacket.PacketID:
            {
                packet = new TransitionToScenePacket();
                shouldPropagate = Server;
                break;
            }
            case BuildOrUpgradePacket.PacketID:
            {
                packet = new BuildOrUpgradePacket();
                shouldPropagate = Server;
                break;
            }
            case DayNightPacket.PacketID:
            {
                packet = new DayNightPacket();
                shouldPropagate = Server;
                break;
            }
            case EnemySpawnPacket.PacketID:
            {
                if (Server)
                {
                    Plugin.Log.LogWarning($"Received unauthorized spawn packet from {sender}.");
                    return;
                }
                
                packet = new EnemySpawnPacket();
                break;
            }
            case DamagePacket.PacketID:
            {
                if (Server)
                {
                    Plugin.Log.LogWarning($"Received unauthorized damage packet from {sender}.");
                    return;
                }
                
                packet = new DamagePacket();
                break;
            }
            case HealPacket.PacketID:
            {
                if (Server)
                {
                    Plugin.Log.LogWarning($"Received unauthorized damage packet from {sender}.");
                    return;
                }
                
                packet = new HealPacket();
                break;
            }
            case ScaleHpPacket.PacketID:
            {
                if (Server)
                {
                    Plugin.Log.LogWarning($"Received unauthorized damage packet from {sender}.");
                    return;
                }
                
                packet = new ScaleHpPacket();
                break;
            }
            case PositionPacket.PacketID:
            {
                if (Server)
                {
                    Plugin.Log.LogWarning($"Received unauthorized position packet from {sender}.");
                    return;
                }
                
                packet = new PositionPacket();
                break;
            }
            case RespawnPacket.PacketID:
            {
                if (Server)
                {
                    Plugin.Log.LogWarning($"Received unauthorized respawn packet from {sender}.");
                    return;
                }
                
                packet = new RespawnPacket();
                break;
            }
            case PacketId.CommandAddPacket:
                if (Server)
                {
                    Plugin.Log.LogWarning($"Received unauthorized command add packet from {sender}.");
                    return;
                }

                packet = new CommandAddPacket();
                break;
            case PacketId.CommandPlacePacket:
            {
                if (Server)
                {
                    Plugin.Log.LogWarning($"Received unauthorized command place packet from {sender}.");
                    return;
                }

                packet = new CommandPlacePacket();
                break;
            }
            case PacketId.CommandHoldPositionPacket:
            {
                if (Server)
                {
                    Plugin.Log.LogWarning($"Received unauthorized command hold position packet from {sender}.");
                    return;
                }

                packet = new CommandHoldPositionPacket();
                break;
            }
            case PacketId.ManualAttack:
            {
                packet = new ManualAttackPacket();
                shouldPropagate = true;
                break;
            }
            case PacketId.BalancePacket:
            {
                packet = new BalancePacket();
                shouldPropagate = true;
                break;
            }
            case PacketId.SpawnCoinPacket:
            {
                packet = new SpawnCoinPacket();
                shouldPropagate = true;
                break;
            }
            case PacketId.PeerSyncPacket:
            {
                packet = new PeerSyncPacket();
                break;
            }
            case PacketId.ApprovalPacket:
            {
                packet = new ApprovalPacket();
                break;
            }
            case PacketId.DisconnectPacket:
            {
                packet = new DisconnectPacket();
                break;
            }
            default:
                Plugin.Log.LogWarning($"Received unknown packet {type} from {sender} containing {buffer.Data.Length} bytes.");
                break;
        }

        if (packet == null)
        {
            return;
        }
        
        packet.Receive(buffer);
        if (shouldPropagate && Server)
        {
            Send(packet);
        }
        
        PacketHandler.HandlePacket(sender, packet);
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t request)
    {
        Plugin.Log.LogInfo($"Got lobby join request {request.m_steamIDLobby} : {request.m_steamIDFriend}");
        ConnectLobby(request.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t entered, bool ioFailure)
    {
        if (ioFailure)
        {
            // TOOD: Show error
            Plugin.Log.LogInfo("IO error encountered");
            return;
        }
        
        if (entered.m_EChatRoomEnterResponse != (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
        {
            // TOOD: Show error
            Plugin.Log.LogInfo($"Failed to join lobby with code {entered.m_EChatRoomEnterResponse}");
            Local();
            return;
        }
        
        // TODO: Figure out password with invite.
        Connect(new CSteamID(entered.m_ulSteamIDLobby), null);
        // Currently we only allow joining a lobby if we are in level select.
        SceneTransitionManagerPatch.DisableTransitionHook = true;
        SceneTransitionManager.instance.TransitionFromNullToLevelSelect();
        SceneTransitionManagerPatch.DisableTransitionHook = false;
    }

    private void OnSessionRequest(SteamNetworkingMessagesSessionRequest_t request)
    {
        Plugin.Log.LogInfo($"Received session request {request.m_identityRemote.GetSteamID().m_SteamID}");
        if (_peers.Count < MaxPlayers)
        {
            Plugin.Log.LogInfo($"Server Full");
            return;
        }

        if (!Server)
        {
            Plugin.Log.LogInfo($"Not a Server");
            return;
        }

        var found = false;
        for (int i = 0, count = SteamMatchmaking.GetNumLobbyMembers(_lobby); i < count; ++i)
        {
            var member = SteamMatchmaking.GetLobbyMemberByIndex(_lobby, i);
            if (request.m_identityRemote.GetSteamID() == member)
            {
                found = true;
                break;
            }
        }

        if (!found)
        {
            Plugin.Log.LogInfo($"Not in lobby");
            return;
        }

        Plugin.Log.LogInfo($"Accepted {request.m_identityRemote.GetSteamID().m_SteamID}");
        SteamNetworkingMessages.AcceptSessionWithUser(ref request.m_identityRemote);
        _peers.Add(request.m_identityRemote.GetSteamID());
    }

    private void OnLobbyChatUpdate(LobbyChatUpdate_t update)
    {
        if (!Online)
        {
            return;
        }

        if ((update.m_rgfChatMemberStateChange & (uint)EChatMemberStateChange.k_EChatMemberStateChangeEntered) != 0)
        {
            return;
        }
        
        Plugin.Log.LogInfo($"Player {update.m_ulSteamIDUserChanged} left");

        var id = new CSteamID(update.m_ulSteamIDUserChanged);
        if (_players.ContainsKey(id))
        {
            Plugin.Instance.PlayerManager.Remove(_players[id].Id);
            _players.Remove(id);
        }

        _peers.Remove(id);
        if (_owner == id)
        {
            MigrateServer();
        }
    }

    private void MigrateServer()
    {
        var owner = SteamMatchmaking.GetLobbyOwner(_lobby);
        Plugin.Log.LogInfo($"Host disconnected migrating server to {owner.m_SteamID}");
        if (owner == SteamUser.GetSteamID())
        {
            // We are now the host.
            Server = true;
        }
        else
        {
            // Connect to new host.
            Connect(owner, _password);
        }
    }
}