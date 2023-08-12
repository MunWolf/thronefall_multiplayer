using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Steamworks;
using ThronefallMP.Components;
using ThronefallMP.Network.Packets;
using ThronefallMP.Network.Packets.Game;
using ThronefallMP.Network.Packets.Sync;
using ThronefallMP.Network.Sync;
using ThronefallMP.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThronefallMP.Network;

public enum Channel
{
    NetworkManagement,
    Player,
    SyncPlayer,
    Resources,
    Game,
}

public class Network : MonoBehaviour
{
    public delegate void ChatMessage(string user, string message);
    public event ChatMessage OnReceivedChatMessage;

    public IEnumerable<CSteamID> Peers => _peers;
    
    private const int MaxMessages = 20;
    
    private Callback<LobbyChatUpdate_t> _lobbyChatUpdate;
    private Callback<GameLobbyJoinRequested_t> _lobbyJoinRequested;
    private Callback<LobbyChatMsg_t> _lobbyChatMessage;
    
    private Callback<SteamNetworkingMessagesSessionRequest_t> _sessionRequestCallback;
    private Callback<SteamNetworkingMessagesSessionFailed_t> _sessionFailed;
    
    private CallResult<LobbyEnter_t> _lobbyEnterResult;

    private readonly PlayerNetworkData.Shared _latestLocalData = new PlayerNetworkData.Shared();
    private readonly HashSet<CSteamID> _pendingPeers = new();
    private readonly HashSet<CSteamID> _peers = new();
    private readonly Dictionary<(int player, Channel channel), int> _lastOrderedPackages = new();
    private readonly IntPtr[] _messages = new IntPtr[MaxMessages];

    public int MaxPlayers { get; set; }
    public bool Authority => Server || !Online;
    public bool Server { get; private set; }
    public bool Online { get; private set; }
    public static CSteamID SteamId => SteamUser.GetSteamID();

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
        Plugin.Log.LogInfoFiltered("Network", $"Comparing {password} with {_password}");
        
        return string.IsNullOrEmpty(_password) || _password == password;
    }

    public void AddPlayer(CSteamID id)
    {
        _pendingPeers.Remove(id);
        _peers.Add(id);
        var player = Plugin.Instance.PlayerManager.CreateOrGet(id, Plugin.Instance.PlayerManager.GenerateID());
        player.SpawnID = Plugin.Instance.PlayerManager.GetAllPlayers().Max(p => p.SpawnID) + 1;
        Plugin.Instance.PlayerManager.InstantiatePlayer(player, player.SpawnLocation);
        
        var packet = new PeerListPacket();
        Plugin.Log.LogInfoFiltered("Network", $"Building peer sync");
        foreach (var data in Plugin.Instance.PlayerManager.GetAllPlayers())
        {
            Plugin.Log.LogInfoFiltered("Network", $" {data.SteamID}:{data.Id} -> {data.Object.transform.position}");
            packet.Players.Add(new PeerListPacket.PlayerData
            {
                Id = data.Id,
                SteamId = data.SteamID,
                SpawnId = data.SpawnID,
                Position = data.Object.transform.position
            });
        }
        
        Plugin.Log.LogInfoFiltered("Network", $"Sending peer sync");
        Send(packet);
    }

    public void KickPeer(CSteamID id, DisconnectPacket.Reason reason)
    {
        var player = Plugin.Instance.PlayerManager.Get(id);
        if (player != null)
        {
            Plugin.Log.LogInfoFiltered("Network", $"Player {id.m_SteamID} kicked with reason {reason}");
            Plugin.Instance.PlayerManager.Remove(player.Id);
        }
        
        _peers.Remove(id);
        _pendingPeers.Remove(id);
        var sid = new SteamNetworkingIdentity();
        sid.SetSteamID(id);
        var packet = new DisconnectPacket()
        {
            DisconnectReason = reason
        };
        SendSingle(packet, sid);
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
        if (_peers.Count > 0 || _pendingPeers.Count > 0)
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
                    if (_peers.Contains(message.m_identityPeer.GetSteamID()))
                    {
                        var buffer = new Buffer(message.m_cbSize);
                        Marshal.Copy(message.m_pData, buffer.Data, 0, buffer.Data.Length);
                        HandlePacket(ref message.m_identityPeer, buffer, PacketTypes);
                    }
                    else if (_pendingPeers.Contains(message.m_identityPeer.GetSteamID()))
                    {
                        var buffer = new Buffer(message.m_cbSize);
                        Marshal.Copy(message.m_pData, buffer.Data, 0, buffer.Data.Length);
                        HandlePacket(ref message.m_identityPeer, buffer, PendingPeerPacketTypes);
                    }
                    
                    SteamNetworkingMessage_t.Release(_messages[i]);
                }
            }
        }

        if (Online)
        {
            BaseSync.UpdateSyncs();
        }
        
        var player = Plugin.Instance.PlayerManager.LocalPlayer;
        if (Server || player == null || player.Shared == _latestLocalData)
        {
            return;
        }
        
        var packet = new ClientSyncPacket
        {
            PlayerID = Plugin.Instance.PlayerManager.LocalId,
            Position = player.Object.transform.position,
            Data = player.Shared
        };
        Send(packet);
        _latestLocalData.Set(player.Shared);
    }

    private void CloseLobby()
    {
        BaseSync.ResetSyncs();
        PacketHandler.AwaitingConnectionApproval = false;
        if (_lobby.IsValid())
        {
            Plugin.Log.LogInfoFiltered("Network", $"Leaving lobby {_lobby.m_SteamID}");
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

        _password = null;
        _lastOrderedPackages.Clear();
    }
    
    public void Local()
    {
        CloseLobby();
        
        Plugin.Log.LogInfoFiltered("Network", "Switched to Local");
        Server = true;
        Online = false;
        
        Plugin.Instance.PlayerManager.Clear();
        Plugin.Instance.PlayerManager.LocalId = Plugin.Instance.PlayerManager.GenerateID();
        var player = Plugin.Instance.PlayerManager.CreateOrGet(CSteamID.Nil, Plugin.Instance.PlayerManager.LocalId);
        player.SpawnID = 0;
        Plugin.Instance.PlayerManager.InstantiatePlayer(player, player.SpawnLocation);
    }

    public void Host(CSteamID lobby, string password)
    {
        CloseLobby();
        
        Plugin.Log.LogInfoFiltered("Network", $"Switched to Host in lobby {lobby.m_SteamID} server {SteamUser.GetSteamID()}");
        _password = password;
        _lobby = lobby;
        _owner = SteamUser.GetSteamID();
        Server = true;
        Online = true;
        Plugin.Instance.PlayerManager.Clear();
        Plugin.Instance.PlayerManager.LocalId = Plugin.Instance.PlayerManager.GenerateID();
        var player = Plugin.Instance.PlayerManager.CreateOrGet(_owner, Plugin.Instance.PlayerManager.LocalId);
        player.SpawnID = 0;
        Plugin.Instance.PlayerManager.InstantiatePlayer(player, player.SpawnLocation);
        SteamMatchmaking.SetLobbyJoinable(lobby, true);
    }

    public void ConnectLobby(CSteamID lobby, string password)
    {
        CloseLobby();
        _password = password;
        _lobbyEnterResult.Set(SteamMatchmaking.JoinLobby(lobby));
    }

    private void Connect(CSteamID lobby, string password)
    {
        Plugin.Log.LogInfoFiltered("Network", "Switched to Connect");
        _password = password;
        _lobby = lobby;
        Server = false;
        Online = true;
        Plugin.Instance.PlayerManager.Clear();
        _owner = SteamMatchmaking.GetLobbyOwner(lobby);
        
        Plugin.Log.LogInfoFiltered("Network", $"Sending approval packet to server {_owner.m_SteamID}");
        PacketHandler.AwaitingConnectionApproval = true;
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
        Plugin.Log.LogInfoFiltered("Network", $"Scene loaded '{scene.name}'");
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

        Plugin.Log.LogInfoFiltered("Network", $"Can join lobby: {SceneManager.GetSceneByName("_LevelSelect").isLoaded}");
        SteamMatchmaking.SetLobbyJoinable(_lobby, SceneManager.GetSceneByName("_LevelSelect").isLoaded);
    }

    public void Send(BasePacket basePacket, bool handleLocal = false, SteamNetworkingIdentity except = new())
    {
        if (Online)
        {
            var buffer = new Buffer();
            Plugin.Log.LogDebugFiltered("Network", $"Writing packet '{basePacket.TypeID}'");
            buffer.Write((int)basePacket.TypeID);
            basePacket.Send(buffer);
            if (Ext.LogDebugFiltered("Network"))
            {
                Plugin.Log.LogDebug($"{buffer.Data.Length}:{BitConverter.ToString(buffer.Data).Replace('-', ' ')}");
            }
            
            var sid = new SteamNetworkingIdentity();
            foreach (var peer in _peers)
            {
                sid.SetSteamID(peer);
                Send(buffer, sid, basePacket.DeliveryMask, basePacket.Channel);
            }
        }

        if (handleLocal)
        {
            var id = new SteamNetworkingIdentity();
            if (SteamManager.Initialized)
            {
                id.SetSteamID(SteamUser.GetSteamID());
            }
            
            PacketHandler.HandlePacket(id, basePacket);
        }
    }

    public void SendSingle(BasePacket basePacket, SteamNetworkingIdentity target)
    {
        var buffer = new Buffer();
        buffer.Write((int)basePacket.TypeID);
        basePacket.Send(buffer);
        Send(buffer, target, basePacket.DeliveryMask, basePacket.Channel);
    }

    private static void Send(Buffer buffer, SteamNetworkingIdentity target, int flags, Channel channel)
    {
        if (target.IsInvalid())
        {
            Plugin.Log.LogInfoFiltered("Network", $"Trying to send with an invalid id '{target.GetSteamID().m_SteamID}'");
            Plugin.Log.LogInfoFiltered("Network", Environment.StackTrace);
            return;
        }

        var handle = GCHandle.Alloc(buffer.Data, GCHandleType.Pinned);
        var pointer = handle.AddrOfPinnedObject();
        
        var result = SteamNetworkingMessages.SendMessageToUser(
            ref target,
            pointer,
            (uint)buffer.WriteHead,
            flags,
            (int)channel
        );
        handle.Free();

        if (result != EResult.k_EResultOK)
        {
            Plugin.Log.LogWarningFiltered("Network", $"SendMessage returned error '{result}'");
        }
    }

    private static readonly Dictionary<PacketId, Type> PendingPeerPacketTypes = new()
    {
        { ApprovalPacket.PacketID, typeof(ApprovalPacket) },
    };

    private static readonly Dictionary<PacketId, Type> PacketTypes = new()
    {
        { DisconnectPacket.PacketID, typeof(DisconnectPacket) },
        { PeerListPacket.PacketID, typeof(PeerListPacket) },
        
        { SyncCheckPacket.PacketID, typeof(SyncCheckPacket) },
        { SyncGeneralPacket.PacketID, typeof(SyncGeneralPacket) },
        { SyncPlayerPacket.PacketID, typeof(SyncPlayerPacket) },
        
        { BuildOrUpgradePacket.PacketID, typeof(BuildOrUpgradePacket) },
        { CancelBuildPacket.PacketID, typeof(CancelBuildPacket) },
        { ClientSyncPacket.PacketID, typeof(ClientSyncPacket) },
        { CommandAddPacket.PacketID, typeof(CommandAddPacket) },
        { CommandPlacePacket.PacketID, typeof(CommandPlacePacket) },
        { CommandHoldPositionPacket.PacketID, typeof(CommandHoldPositionPacket) },
        { ConfirmBuildPacket.PacketID, typeof(ConfirmBuildPacket) },
        { DamagePacket.PacketID, typeof(DamagePacket) },
        { DayNightPacket.PacketID, typeof(DayNightPacket) },
        { EnemySpawnPacket.PacketID, typeof(EnemySpawnPacket) },
        { HealPacket.PacketID, typeof(HealPacket) },
        { ManualAttackPacket.PacketID, typeof(ManualAttackPacket) },
        { PositionPacket.PacketID, typeof(PositionPacket) },
        { RespawnPacket.PacketID, typeof(RespawnPacket) },
        { ScaleHpPacket.PacketID, typeof(ScaleHpPacket) },
        { TransitionToScenePacket.PacketID, typeof(TransitionToScenePacket) },
    };

    private int GetLastOrderedPackage(int player, Channel channel)
    {
        return _lastOrderedPackages.TryGetValue((player, channel), out var value) ? value : -1;
    }

    private void SetLastOrderedPackage(int player, Channel channel, int value)
    {
        _lastOrderedPackages[(player, channel)] = value;
    }
    
    private void HandlePacket(ref SteamNetworkingIdentity sender, Buffer buffer, Dictionary<PacketId, Type> types)
    {
        var type = (PacketId)buffer.ReadInt32();
        types.TryGetValue(type, out var objectType);
        if (objectType?.GetConstructor(Type.EmptyTypes)?.Invoke(Array.Empty<object>()) is not BasePacket basePacket)
        {
            Plugin.Log.LogInfoFiltered("Network", $"Received unknown packet (type: '{type}', size: {buffer.Data.Length})");
            return;
        }
        
        Plugin.Log.LogDebugFiltered("Network", $"Reading packet '{type}'");
        basePacket.Receive(buffer);
        if (Ext.LogDebugFiltered("Network"))
        {
            Plugin.Log.LogDebug($"{buffer.Data.Length}:{BitConverter.ToString(buffer.Data).Replace('-', ' ')}");
        }
        if (basePacket is BaseOrderedPacket orderedPacket)
        {
            var order = orderedPacket.GetOrder();
            var lastCount = GetLastOrderedPackage(order.player, orderedPacket.Channel);
            if (lastCount >= order.count)
            {
                // Packet arrived late, discard it.
                return;
            }

            SetLastOrderedPackage(order.player, orderedPacket.Channel, order.count);
        }
        
        if (basePacket.CanHandle(sender.GetSteamID()))
        {
            PacketHandler.HandlePacket(sender, basePacket);
        }
        else
        {
            Plugin.Log.LogInfoFiltered("Network", $"Received unauthorized packet from '{sender.GetSteamID().m_SteamID}' (type: '{type}', size: {buffer.Data.Length})");
        }

        if (Server && basePacket.ShouldPropagate)
        {
            Send(basePacket, false, sender);
        }
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t request)
    {
        Plugin.Log.LogInfoFiltered("Network", $"Got lobby join request {request.m_steamIDLobby} : {request.m_steamIDFriend}");
        ConnectLobby(request.m_steamIDLobby, null);
    }

    private void OnLobbyEntered(LobbyEnter_t entered, bool ioFailure)
    {
        if (ioFailure)
        {
            // TOOD: Show error
            Plugin.Log.LogInfoFiltered("Network", "IO error encountered");
            return;
        }
        
        if (entered.m_EChatRoomEnterResponse != (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
        {
            // TOOD: Show error
            Plugin.Log.LogInfoFiltered("Network", $"Failed to join lobby with code {entered.m_EChatRoomEnterResponse}");
            Local();
            return;
        }

        var id = new CSteamID(entered.m_ulSteamIDLobby);
        if (SteamMatchmaking.GetLobbyData(id, "password") == "yes" && _password == null)
        {
            UIManager.CreatePasswordDialog(
                (password) =>
                {
                    _password = password;
                    Connect(id, _password);
                },
                CloseLobby
            );
        }
        else
        {
            Connect(id, _password);
        }
    }
    
    private void OnSessionRequest(SteamNetworkingMessagesSessionRequest_t request)
    {
        Plugin.Log.LogInfoFiltered("Network", $"Received session request {request.m_identityRemote.GetSteamID().m_SteamID}");
        if (_peers.Count < MaxPlayers)
        {
            Plugin.Log.LogInfoFiltered("Network", $"Server Full");
            return;
        }

        if (!Server)
        {
            Plugin.Log.LogInfoFiltered("Network", $"Not a Server");
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
            Plugin.Log.LogInfoFiltered("Network", $"Not in lobby");
            return;
        }

        Plugin.Log.LogInfoFiltered("Network", $"Accepted {request.m_identityRemote.GetSteamID().m_SteamID}");
        _pendingPeers.Add(request.m_identityRemote.GetSteamID());
        SteamNetworkingMessages.AcceptSessionWithUser(ref request.m_identityRemote);
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
        
        Plugin.Log.LogInfoFiltered("Network", $"Player {update.m_ulSteamIDUserChanged} left");

        var id = new CSteamID(update.m_ulSteamIDUserChanged);
        var player = Plugin.Instance.PlayerManager.Get(id);
        if (player != null)
        {
            Plugin.Log.LogInfoFiltered("Network", $"Destroying player {player.Id}");
            Plugin.Instance.PlayerManager.Remove(player.Id);
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
        Plugin.Log.LogInfoFiltered("Network", $"Host disconnected migrating server to {owner.m_SteamID}");
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

    public bool IsServer(CSteamID id)
    {
        return SteamMatchmaking.GetLobbyOwner(_lobby) == id;
    }
}