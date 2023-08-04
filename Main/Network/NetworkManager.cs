using System;
using System.Collections.Generic;
using System.Linq;
using Lidgren.Network;
using ThronefallMP.Components;
using ThronefallMP.NetworkPackets;
using ThronefallMP.NetworkPackets.Game;
using ThronefallMP.Patches;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ThronefallMP.Network;

public class NetworkManager
{
    private class NetworkPlayer
    {
        public int ID;
        public PlayerNetworkData Data;
        public NetConnection Connection = null;
    }
    
    public int LocalPlayer { get; set; } = -1;
    public PlayerNetworkData LocalPlayerData => GetPlayerData(LocalPlayer);
    public bool Online { get; private set; }
    public bool Server { get; private set; } = true;
    public int ActivePort { get; private set; }

    private readonly Dictionary<long, NetworkPlayer> _remoteIdToPlayer = new();
    private readonly Dictionary<int, NetworkPlayer> _playerIdToPlayer = new();
    private readonly List<NetConnection> _peers = new();
    
    private GameObject _playerPrefab;
    private bool _playerUpdateQueued;
    private PlayerNetworkData.Shared? _latestLocalData;

    private NetServer _server;
    private readonly NetClient _client;

    private NetPeer Peer => Server ? _server : _client;

    public NetworkManager()
    {
        var config = new NetPeerConfiguration("thronefall_mp");
        _client = new NetClient(config);
    }

    private void Stop()
    {
        switch (Online)
        {
            case true when Server:
                _server.Shutdown("Server shutting down");
                break;
            case true when !Server:
                _client.Shutdown("Client disconnecting");
                break;
        }

        Online = false;
        Server = true;
    }

    public void Local()
    {
        Plugin.Log.LogInfo($"Switching to Local");
        Stop();
        ClearData();
        Online = false;
        Server = true;
        LocalPlayer = GeneratePlayerID();
        CreatePlayer(LocalPlayer);
    }
    
    public void Host(int port)
    {
        Plugin.Log.LogInfo($"Hosting on {port}");
        Stop();
        ClearData();
        Online = true;
        Server = true;
        ActivePort = port;
        
        var config = new NetPeerConfiguration("thronefall_mp")
        {
            EnableUPnP = true,
            Port = port
        };
        
        config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
        _server = new NetServer(config);
        _server.Start();
        _server.UPnP.ForwardPort(config.Port, "Thronefall Multiplayer");
        
        LocalPlayer = GeneratePlayerID();
        CreatePlayer(LocalPlayer);
    }
    
    public void Connect(string address, int port, ApprovalPacket packet)
    {
        Plugin.Log.LogInfo($"Attempting to connect to {address}:{port}");
        Stop();
        Online = true;
        Server = false;
        ActivePort = port;
        var message = _client.CreateMessage();
        packet.Send(message);
        _client.Start();
        _client.Connect(address, port, message);
    }
    
    public void Send(IPacket packet, bool handleLocal = false, long? except = null)
    {
        if (Online && _peers.Count > 0)
        {
            var writer = CreateMessage();
            writer.Write((int)packet.TypeID);
            packet.Send(writer);
            if (except != null)
            {
                var peers = _peers.ToList();
                peers.RemoveAll((p) => p.RemoteUniqueIdentifier == except);
                _server.SendMessage(writer, peers, packet.Delivery, packet.Channel);
            }
            else
            {
                _server.SendMessage(writer, _peers, packet.Delivery, packet.Channel);
            }
        }

        if (handleLocal)
        {
            PacketHandler.HandlePacket(packet);
        }
    }
    
    public void Send(NetConnection connection, IPacket packet)
    {
        var writer = CreateMessage();
        writer.Write((int)packet.TypeID);
        packet.Send(writer);
        _server.SendMessage(writer, connection, packet.Delivery, packet.Channel);
    }

    private NetOutgoingMessage CreateMessage()
    {
        return Server ? _server.CreateMessage() : _client.CreateMessage();
    }

    public void InitializeDefaultPlayer(GameObject player)
    {
        if (_playerPrefab != null)
        {
            Object.Destroy(_playerPrefab);
        }
        
        player.SetActive(false);
        _playerPrefab = Object.Instantiate(player);
        _playerPrefab.SetActive(false);
        var data = _playerPrefab.AddComponent<PlayerNetworkData>();
        data.id = -1;
        _playerPrefab.AddComponent<Identifier>();
        player.SetActive(true);
        Plugin.Log.LogInfo("Initialized player prefab");
    }
    
    public PlayerNetworkData GetPlayerData(int id)
    {
        return _playerIdToPlayer.TryGetValue(id, out var player) ? player.Data : null;
    }
    
    public IEnumerable<PlayerNetworkData> GetAllPlayerData()
    {
        foreach (var pair in _playerIdToPlayer)
        {
            yield return pair.Value.Data;
        }
    }

    public void ReinstantiatePlayers()
    {
        if (PlayerManager.Instance != null)
        {
            foreach (var player in PlayerManager.Instance.RegisteredPlayers)
            {
                PlayerManager.UnregisterPlayer(player);
                Object.Destroy(player.gameObject);
            }
        }

        foreach (var pair in _playerIdToPlayer)
        {
            CreatePlayer(pair.Key);
        }
    }

    private void ClearData()
    {
        _latestLocalData = null;
        _playerIdToPlayer.Clear();
        _remoteIdToPlayer.Clear();
        foreach (var player in PlayerManager.Instance.RegisteredPlayers)
        {
            PlayerManager.UnregisterPlayer(player);
            Object.Destroy(player.gameObject);
        }
    }

    private int GeneratePlayerID()
    {
        int id;
        do { id = Plugin.Random.Next(); }
        while (_playerIdToPlayer.ContainsKey(id));
        return id;
    }

    public void Update()
    {
        var playerData = GetPlayerData(LocalPlayer);
        if (playerData != null && playerData.SharedData != _latestLocalData)
        {
            var packet = new PlayerSyncPacket
            {
                PlayerID = LocalPlayer,
                Data = playerData.SharedData
            };
            Send(packet);
        }
        
        if (!Server || !Online)
        {
            return;
        }
        
        while (_server.ReadMessage() is { } msg)
        {
            HandleMessage(msg);
            _server.Recycle(msg);
        }
        
        if (_playerUpdateQueued)
        {
            var packet = new PeerSyncPacket();
            Plugin.Log.LogInfo($"Sending player list");
            
            foreach (var pair in _playerIdToPlayer)
            {
                packet.Players.Add(new PeerSyncPacket.PlayerData
                {
                    Id = pair.Key,
                    Position = pair.Value.Data.SharedData.Position
                });
                
                Plugin.Log.LogInfo($" {pair.Key} -> {pair.Value.Data.SharedData.Position}");
            }

            foreach (var peer in _peers)
            {
                var player = _remoteIdToPlayer[peer.RemoteUniqueIdentifier];
                packet.LocalPlayer = player.ID;
                Send(player.Connection, packet);
            }
            
            _playerUpdateQueued = false;
        }
    }

    private void HandleMessage(NetIncomingMessage msg)
    {
        switch (msg.MessageType)
        {
            case NetIncomingMessageType.ConnectionApproval:
            {
                var approval = new ApprovalPacket();
                approval.Receive(msg);
                if (approval.Approved && SceneTransitionManagerPatch.InLevelSelect)
                {
                    msg.SenderConnection.Approve();
                    if (Server)
                    {
                        var player = new NetworkPlayer
                        {
                            ID = GeneratePlayerID(),
                            Connection = msg.SenderConnection
                        };

                        Plugin.Log.LogInfo($"Peer {msg.SenderConnection.RemoteUniqueIdentifier} connected assigned to {player.ID}");
                        _remoteIdToPlayer[msg.SenderConnection.RemoteUniqueIdentifier] = player;
                        _playerIdToPlayer[player.ID] = player;
                        CreatePlayer(player.ID);
                        _playerUpdateQueued = true;
                        _peers.Add(msg.SenderConnection);
                    }
                    else
                    {
                        ClearData();
                        Plugin.Log.LogInfo($"Connected to server");
                    }
                }
                else
                {
                    msg.SenderConnection.Deny();
                }
                break;
            }
            case NetIncomingMessageType.Data:
            {
                HandlePacket(msg);
                break;
            }
            case NetIncomingMessageType.VerboseDebugMessage:
            case NetIncomingMessageType.DebugMessage:
            case NetIncomingMessageType.WarningMessage:
            case NetIncomingMessageType.ErrorMessage:
                Console.WriteLine(msg.ReadString());
                break;
            case NetIncomingMessageType.StatusChanged:
            {
                var status = (NetConnectionStatus)msg.ReadByte();
                if (status == NetConnectionStatus.Disconnected)
                {
                    Local();
                }
                break;
            }
            case NetIncomingMessageType.DiscoveryRequest: // TODO: Implement discovery.
            case NetIncomingMessageType.DiscoveryResponse:
                
            case NetIncomingMessageType.UnconnectedData:
            case NetIncomingMessageType.ConnectionLatencyUpdated:
            case NetIncomingMessageType.NatIntroductionSuccess:
            case NetIncomingMessageType.Error:
            case NetIncomingMessageType.Receipt:
            default:
                break;
        }
    }

    public GameObject CreatePlayer(int id)
    {
        var newPlayer = Object.Instantiate(_playerPrefab);
        newPlayer.transform.position = Utils.GetSpawnLocation(PlayerMovementPatch.SpawnLocation, id);
        newPlayer.SetActive(true);
        var data = newPlayer.GetComponent<PlayerNetworkData>();
        data.id = id;
        data.SharedData.Position = newPlayer.transform.position;
        var identifier = newPlayer.GetComponent<Identifier>();
        identifier.SetIdentity(IdentifierType.Player, id);
        if (!_playerIdToPlayer.ContainsKey(id))
        {
            _playerIdToPlayer.Add(id, new NetworkPlayer());
        }

        _playerIdToPlayer[id].ID = id;
        _playerIdToPlayer[id].Data = data;
        return newPlayer;
    }

    private void HandlePacket(NetIncomingMessage reader)
    {
        var player = _remoteIdToPlayer[reader.SenderConnection.RemoteUniqueIdentifier];
        var type = (PacketId)reader.ReadInt32();
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
                    Plugin.Log.LogWarning($"Received unauthorized spawn packet from {player.ID}.");
                    return;
                }
                
                packet = new EnemySpawnPacket();
                break;
            }
            case DamagePacket.PacketID:
            {
                if (Server)
                {
                    Plugin.Log.LogWarning($"Received unauthorized damage packet from {player.ID}.");
                    return;
                }
                
                packet = new DamagePacket();
                break;
            }
            case HealPacket.PacketID:
            {
                if (Server)
                {
                    Plugin.Log.LogWarning($"Received unauthorized damage packet from {player.ID}.");
                    return;
                }
                
                packet = new HealPacket();
                break;
            }
            case ScaleHpPacket.PacketID:
            {
                if (Server)
                {
                    Plugin.Log.LogWarning($"Received unauthorized damage packet from {player.ID}.");
                    return;
                }
                
                packet = new ScaleHpPacket();
                break;
            }
            case PositionPacket.PacketID:
            {
                if (Server)
                {
                    Plugin.Log.LogWarning($"Received unauthorized position packet from {player.ID}.");
                    return;
                }
                
                packet = new PositionPacket();
                break;
            }
            case RespawnPacket.PacketID:
            {
                if (Server)
                {
                    Plugin.Log.LogWarning($"Received unauthorized respawn packet from {player.ID}.");
                    return;
                }
                
                packet = new RespawnPacket();
                break;
            }
            case PacketId.CommandAddPacket:
                if (Server)
                {
                    Plugin.Log.LogWarning($"Received unauthorized command add packet from {player.ID}.");
                    return;
                }

                packet = new CommandAddPacket();
                break;
            case PacketId.CommandPlacePacket:
            {
                if (Server)
                {
                    Plugin.Log.LogWarning($"Received unauthorized command place packet from {player.ID}.");
                    return;
                }

                packet = new CommandPlacePacket();
                break;
            }
            case PacketId.CommandHoldPositionPacket:
            {
                if (Server)
                {
                    Plugin.Log.LogWarning($"Received unauthorized command hold position packet from {player.ID}.");
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
            default:
                Plugin.Log.LogWarning($"Received unknown packet {type} from {player.ID} containing {reader.LengthBytes} bytes.");
                break;
        }

        if (packet == null)
        {
            return;
        }
        
        packet.Receive(reader);
        if (shouldPropagate && Server)
        {
            Send(packet);
        }
        
        PacketHandler.HandlePacket(packet);
    }
}