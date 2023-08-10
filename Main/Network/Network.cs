using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Steamworks;
using ThronefallMP.Components;
using ThronefallMP.NetworkPackets;
using ThronefallMP.NetworkPackets.Game;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThronefallMP.Network;

public class Network : MonoBehaviour
{
    public enum Channel
    {
        General,
        NetworkManagement
    }
    
    private const int MaxMessages = 20;
    
    private Callback<LobbyChatUpdate_t> _lobbyChatUpdate;
    
    private Callback<SteamNetworkingMessagesSessionRequest_t> _sessionRequestCallback;
    private Callback<SteamNetworkingMessagesSessionFailed_t> _sessionFailed;

    private readonly PlayerNetworkData.Shared _latestLocalData = new PlayerNetworkData.Shared();
    private readonly List<SteamNetworkingIdentity> _peers = new();
    private readonly Dictionary<SteamNetworkingIdentity, PlayerManager.Player> _players = new();
    private readonly IntPtr[] _messages = new IntPtr[MaxMessages];

    public int MaxPlayers { get; set; }
    public bool Authority => Server || !Online;
    public bool Server { get; private set; }
    public bool Online { get; private set; }

    private CSteamID _lobby;
    private CSteamID _owner;
    private string _password;
    
    public void Awake()
    {
        _sessionRequestCallback = Callback<SteamNetworkingMessagesSessionRequest_t>.Create(OnSessionRequest);
        _sessionFailed = Callback<SteamNetworkingMessagesSessionFailed_t>.Create(OnSessionFailed);
        _lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
        SceneManager.sceneLoaded += OnSceneChanged;
    }

    private void OnSessionRequest(SteamNetworkingMessagesSessionRequest_t request)
    {
        if (_peers.Count < MaxPlayers ||
            request.m_identityRemote.m_eType != ESteamNetworkingIdentityType.k_ESteamNetworkingIdentityType_SteamID)
        {
            return;
        }

        if (!Server)
        {
            return;
        }

        SteamNetworkingMessages.AcceptSessionWithUser(ref request.m_identityRemote);
    }

    private void OnLobbyChatUpdate(LobbyChatUpdate_t update)
    {
        if (!Server || !Online)
        {
            return;
        }
        
        if ((update.m_rgfChatMemberStateChange & (uint)EChatMemberStateChange.k_EChatMemberStateChangeEntered) == 0)
        {
            var identity = new SteamNetworkingIdentity();
            identity.SetSteamID64(update.m_ulSteamIDUserChanged);
            if (_owner == identity.GetSteamID())
            {
                // TODO: Need to migrate server to new owner.
            }
            
            if (_players.ContainsKey(identity))
            {
                Destroy(_players[identity].Object);
                _players.Remove(identity);
            }

            _peers.Remove(identity);
        }
    }

    public bool Authenticate(string password)
    {
        return _password == null || _password == password;
    }
    
    public void AddPlayer(SteamNetworkingIdentity id)
    {
        _peers.Add(id);
        var player = Plugin.Instance.PlayerManager.Create(Plugin.Instance.PlayerManager.GenerateID());
        _players[id] = player;
        var packet = new PeerSyncPacket();
        Plugin.Log.LogInfo($"Sending player list");
        
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
            packet.LocalPlayer = pair.Value.Id;
            SendSingle(packet, pair.Key);
        }
    }
    
    private void OnSessionFailed(SteamNetworkingMessagesSessionFailed_t failed)
    {
        _peers.Remove(failed.m_info.m_identityRemote);
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
                    message.Release();
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

    public void Local()
    {
        Plugin.Log.LogInfo("Switched to Local");
        _password = null;
        Server = true;
        Online = false;
        
        if (_lobby.IsValid())
        {
            SteamMatchmaking.LeaveLobby(_lobby);
            foreach (var peer in _peers)
            {
                var peerMutable = peer;
                SteamNetworkingMessages.CloseSessionWithUser(ref peerMutable);
            }
            _lobby.Clear();
        }
        
        Plugin.Instance.PlayerManager.Clear();
        Plugin.Instance.PlayerManager.LocalId = Plugin.Instance.PlayerManager.GenerateID();
        Plugin.Instance.PlayerManager.Create(Plugin.Instance.PlayerManager.LocalId);
    }

    public void Host(CSteamID lobby, string password)
    {
        Plugin.Log.LogInfo("Switched to Host");
        _password = password;
        _lobby = lobby;
        _owner = SteamUser.GetSteamID();
        Server = true;
        Online = true;
        Plugin.Instance.PlayerManager.Clear();
        Plugin.Instance.PlayerManager.LocalId = Plugin.Instance.PlayerManager.GenerateID();
        Plugin.Instance.PlayerManager.Create(Plugin.Instance.PlayerManager.LocalId);
        SteamMatchmaking.SetLobbyGameServer(lobby, 0, 0, _owner);
        SteamMatchmaking.SetLobbyJoinable(lobby, true);
    }

    public bool Connect(CSteamID lobby, string password)
    {
        Plugin.Log.LogInfo("Switched to Connect");
        _password = password;
        _lobby = lobby;
        Server = false;
        Online = true;
        Plugin.Instance.PlayerManager.Clear();
        if (!SteamMatchmaking.GetLobbyGameServer(lobby, out _, out _, out var id))
        {
            return false;
        }

        var approvalPacket = new ApprovalPacket()
        {
            Password = password
        };

        var identity = new SteamNetworkingIdentity();
        identity.SetSteamID(id);
        SendSingle(approvalPacket, identity);
        return true;
    }

    private void OnSceneChanged(Scene scene, LoadSceneMode mode)
    {
        Plugin.Log.LogInfo($"Scene loaded '{scene.name}'");
        if (scene.name == "_StartMenu")
        {
            Local();
            return;
        }
        
        if (!Server || !Online || !_lobby.IsValid())
        {
            return;
        }

        SteamMatchmaking.SetLobbyJoinable(_lobby, SceneManager.GetSceneByName("_LevelSelect").isLoaded);
    }

    public void Send(IPacket packet, bool handleLocal = false, SteamNetworkingIdentity except = new())
    {
        if (Online)
        {
            var buffer = new Buffer();
            packet.Send(buffer);
            foreach (var peer in _peers)
            {
                Send(buffer, peer, packet.DeliveryMask, packet.Channel);
            }
        }

        if (handleLocal)
        {
            var identity = new SteamNetworkingIdentity();
            if (SteamManager.Initialized)
            {
                identity.SetSteamID(SteamUser.GetSteamID());
            }
            
            PacketHandler.HandlePacket(identity, packet);
        }
    }

    public void SendSingle(IPacket packet, SteamNetworkingIdentity target)
    {
        var buffer = new Buffer();
        packet.Send(buffer);
        Send(buffer, target, packet.DeliveryMask, packet.Channel);
    }

    private static void Send(Buffer buffer, SteamNetworkingIdentity target, int flags, int channel)
    {
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
}