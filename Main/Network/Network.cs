using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using JetBrains.Annotations;
using Steamworks;
using ThronefallMP.Network.Packets;
using ThronefallMP.Network.Packets.Administration;
using ThronefallMP.Network.Packets.Game;
using ThronefallMP.Network.Packets.PlayerCommand;
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
    SyncUnit,
    Resources,
    Game,
}

public class Network : MonoBehaviour
{
    public delegate bool ChatMessageHandler(string user, string message);

    public IEnumerable<CSteamID> Peers => _peers;
    
    private const int MaxMessages = 20;
    
    [UsedImplicitly] private Callback<LobbyChatUpdate_t> _lobbyChatUpdate;
    [UsedImplicitly] private Callback<GameLobbyJoinRequested_t> _lobbyJoinRequested;
    [UsedImplicitly] private Callback<LobbyChatMsg_t> _lobbyChatMessage;
    
    [UsedImplicitly] private Callback<SteamNetworkingMessagesSessionRequest_t> _sessionRequestCallback;
    [UsedImplicitly] private Callback<SteamNetworkingMessagesSessionFailed_t> _sessionFailed;
    
    private CallResult<LobbyEnter_t> _lobbyEnterResult;

    private readonly HashSet<CSteamID> _pendingPeers = new();
    private readonly HashSet<CSteamID> _peers = new();
    private readonly Dictionary<(int player, Channel channel), int> _lastOrderedPackages = new();
    private readonly IntPtr[] _messages = new IntPtr[MaxMessages];
    private List<(int priority, ChatMessageHandler handler)> _messageHandlers = new();

    public int MaxPlayers { get; set; }
    public bool Authority => Server || !Online;
    public bool Server { get; private set; }
    public bool Online { get; private set; }
    public CSteamID Owner { get; private set; }
    public CSteamID Lobby { get; private set; }
    public static CSteamID SteamId => SteamUser.GetSteamID();

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

        var buffer = new Buffer() { Data = _chatBuffer };
        var messageType = buffer.ReadInt32();
        if (messageType == 0)
        {
            var username = SteamFriends.GetFriendPersonaName(user);
            HandleMessage(username, buffer.ReadString());
        }
        else if (messageType == 1 && user == Owner)
        {
            HandleMessage("Server", buffer.ReadString());
        }
    }

    private void HandleMessage(string user, string message)
    {
        foreach (var entry in _messageHandlers)
        {
            if (entry.handler.Invoke(user, message))
            {
                break;
            }
        }
    }
    
    public void SendChatMessage(string message)
    {
        if (!Lobby.IsValid())
        {
            return;
        }

        var buffer = new Buffer();
        buffer.Write(0);
        buffer.Write(message);
        SteamMatchmaking.SendLobbyChatMsg(Lobby, buffer.Data, buffer.WriteHead);
    }
    
    public void SendServerMessage(string message)
    {
        if (!Lobby.IsValid() && Server)
        {
            return;
        }

        var buffer = new Buffer();
        buffer.Write(1);
        buffer.Write(message);
        SteamMatchmaking.SendLobbyChatMsg(Lobby, buffer.Data, buffer.WriteHead);
    }

    public void AddChatMessageHandler(int priority, ChatMessageHandler handler)
    {
        _messageHandlers.Add((priority, handler));
        _messageHandlers.Sort((a, b) => b.priority.CompareTo(a.priority));
    }
    
    public bool Authenticate(string password)
    {
        Plugin.Log.LogInfoFiltered("Network", $"Comparing {password} with {_password}");
        
        return string.IsNullOrEmpty(_password) || _password == password;
    }

    public void AddPlayer(CSteamID id)
    {
        SyncManager.OnConnected(id);
        _peers.Add(id);
        _pendingPeers.Remove(id);
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
    }

    private void LateUpdate()
    {
        // We want to do the syncing at the end of a frame, so we catch any objects marked for destruction.
        if (Online)
        {
            SyncManager.UpdateSyncs();
        }
    }

    private void CloseLobby()
    {
        SyncManager.ResetSyncs();
        if (PacketHandler.AwaitingConnectionApproval)
        {
            UIManager.LobbyListPanel.CloseConnectingDialog();
            PacketHandler.AwaitingConnectionApproval = false;
        }
        
        if (Lobby.IsValid())
        {
            Plugin.Log.LogInfoFiltered("Network", $"Leaving lobby {Lobby.m_SteamID}");
            SteamMatchmaking.LeaveLobby(Lobby);
            foreach (var peer in _peers)
            {
                var sid = new SteamNetworkingIdentity();
                sid.SetSteamID(peer);
                SteamNetworkingMessages.CloseSessionWithUser(ref sid);
            }
            
            _peers.Clear();
            Lobby.Clear();
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
        Lobby = lobby;
        Owner = SteamUser.GetSteamID();
        Server = true;
        Online = true;
        Plugin.Instance.PlayerManager.Clear();
        Plugin.Instance.PlayerManager.LocalId = Plugin.Instance.PlayerManager.GenerateID();
        var player = Plugin.Instance.PlayerManager.CreateOrGet(Owner, Plugin.Instance.PlayerManager.LocalId);
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
        Lobby = lobby;
        Server = false;
        Online = true;
        Plugin.Instance.PlayerManager.Clear();
        Owner = SteamMatchmaking.GetLobbyOwner(lobby);
        
        Plugin.Log.LogInfoFiltered("Network", $"Sending approval packet to server {Owner.m_SteamID}");
        PacketHandler.AwaitingConnectionApproval = true;
        _peers.Add(Owner);
        StartCoroutine(GetApproval(password));
    }

    private IEnumerator GetApproval(string password)
    {
        var approvalPacket = new ApprovalPacket()
        {
            Password = password
        };
            
        var id = new SteamNetworkingIdentity();
        id.SetSteamID(Owner);
        while (PacketHandler.AwaitingConnectionApproval)
        {
            SendSingle(approvalPacket, id);
            yield return new WaitForSeconds(2f);
        }
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
        
        if (!Server || !Online || !Lobby.IsValid())
        {
            return;
        }

        Plugin.Log.LogInfoFiltered("Network", $"Can join lobby: {SceneManager.GetSceneByName("_LevelSelect").isLoaded}");
        SteamMatchmaking.SetLobbyJoinable(Lobby, SceneManager.GetSceneByName("_LevelSelect").isLoaded);
    }

    public void Send(BasePacket basePacket, bool handleLocal = false, SteamNetworkingIdentity except = new())
    {
        if (Online && _peers.Count > 0)
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
        
        { SyncPingPacket.PacketID, typeof(SyncPingPacket) },
        { SyncPongPacket.PacketID, typeof(SyncPongPacket) },
        { SyncPingInfoPacket.PacketID, typeof(SyncPingInfoPacket) },
        { SyncAllyPathfinderPacket.PacketID, typeof(SyncAllyPathfinderPacket) },
        { SyncEnemyPathfinderPacket.PacketID, typeof(SyncEnemyPathfinderPacket) },
        { SyncHpPacket.PacketID, typeof(SyncHpPacket) },
        { SyncLevelDataPacket.PacketID, typeof(SyncLevelDataPacket) },
        { SyncPlayerInputPacket.PacketID, typeof(SyncPlayerInputPacket) },
        { SyncPlayersPacket.PacketID, typeof(SyncPlayersPacket) },
        { SyncPositionPacket.PacketID, typeof(SyncPositionPacket) },
        { SyncResourcePacket.PacketID, typeof(SyncResourcePacket) },
        
        { DamageFeedbackPacket.PacketID, typeof(DamageFeedbackPacket) },
        { DayNightPacket.PacketID, typeof(DayNightPacket) },
        { EnemySpawnPacket.PacketID, typeof(EnemySpawnPacket) },
        { RequestLevelPacket.PacketID, typeof(RequestLevelPacket)},
        
        { BuildOrUpgradePacket.PacketID, typeof(BuildOrUpgradePacket) },
        { CancelBuildPacket.PacketID, typeof(CancelBuildPacket) },
        { CommandAddPacket.PacketID, typeof(CommandAddPacket) },
        { CommandPlacePacket.PacketID, typeof(CommandPlacePacket) },
        { CommandHoldPositionPacket.PacketID, typeof(CommandHoldPositionPacket) },
        { ConfirmBuildPacket.PacketID, typeof(ConfirmBuildPacket) },
        { ManualAttackPacket.PacketID, typeof(ManualAttackPacket) },
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
            UIManager.LobbyListPanel.CloseConnectingDialog();
            Plugin.Log.LogInfoFiltered("Network", "IO error encountered");
            return;
        }
        
        if (entered.m_EChatRoomEnterResponse != (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
        {
            UIManager.LobbyListPanel.CloseConnectingDialog();
            Plugin.Log.LogInfoFiltered("Network", $"Failed to join lobby with code {entered.m_EChatRoomEnterResponse}");
            Local();
            return;
        }

        var id = new CSteamID(entered.m_ulSteamIDLobby);
        if (SteamMatchmaking.GetLobbyData(id, "password") == "yes" && _password == null)
        {
            UIManager.LobbyListPanel.ShowHideConnectingDialog(false);
            UIManager.CreatePasswordDialog(
                (password) =>
                {
                    UIManager.LobbyListPanel.ShowHideConnectingDialog(true);
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

        var id = request.m_identityRemote.GetSteamID();
        if (_peers.Contains(id))
        {
            // We are already connected to the peer, session timed out.
            SteamNetworkingMessages.AcceptSessionWithUser(ref request.m_identityRemote);
            SyncManager.OnConnected(id);
            return;
        }
        
        if (!Server)
        {
            Plugin.Log.LogInfoFiltered("Network", $"Not a Server");
            return;
        }

        var found = false;
        for (int i = 0, count = SteamMatchmaking.GetNumLobbyMembers(Lobby); i < count; ++i)
        {
            var member = SteamMatchmaking.GetLobbyMemberByIndex(Lobby, i);
            if (id == member)
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

        Plugin.Log.LogInfoFiltered("Network", $"Accepted {id.m_SteamID}");
        _pendingPeers.Add(id);
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
        if (Owner == id)
        {
            MigrateServer();
        }
    }

    private void MigrateServer()
    {
        Owner = SteamMatchmaking.GetLobbyOwner(Lobby);
        Plugin.Log.LogInfoFiltered("Network", $"Host disconnected migrating server to {Owner.m_SteamID}");
        if (Owner == SteamUser.GetSteamID())
        {
            // We are now the host.
            HandleMessage("Server", $"You are now hosting");
            Server = true;
        }
        else
        {
            // Connect to new host.
            HandleMessage("Server", $"Host disconnected migrating server to {SteamFriends.GetFriendPersonaName(Owner)}");
            Connect(Owner, _password);
        }
    }

    public bool IsServer(CSteamID id)
    {
        return SteamMatchmaking.GetLobbyOwner(Lobby) == id;
    }
}