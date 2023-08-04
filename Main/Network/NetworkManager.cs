using System;
using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;
using Mono.Nat;
using ThronefallMP.Components;
using ThronefallMP.NetworkPackets;
using ThronefallMP.Patches;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ThronefallMP.Network;

public class NetworkManager
{
    public struct ConnectionResponse
    {
        public bool Succeeded;
        public DisconnectReason Reason;
    }
    
    private class NetworkPeerData
    {
        public int Id;
        public NetPeer Peer;
        public PlayerNetworkData Data;
    }
    
    public int LocalPlayer { get; private set; } = -1;
    public PlayerNetworkData LocalPlayerData => GetPlayerData(LocalPlayer);
    public bool Online { get; private set; }
    public bool Server { get; private set; } = true;
    public int ActivePort { get; private set; }

    private readonly EventBasedNetListener _listener;
    private readonly NetManager _netManager;
    private readonly Dictionary<int, NetworkPeerData> _data = new();
    
    private GameObject _playerPrefab;
    private bool _playerUpdateQueued;
    private PlayerNetworkData.Shared? _latestLocalData;
    private Action<ConnectionResponse> _connectionResponse;

    public NetworkManager()
    {
        NatUtility.DeviceFound += OnDeviceFound;
        _listener = new EventBasedNetListener();
        _netManager = new NetManager(_listener);
        _listener.ConnectionRequestEvent += ConnectionRequestEvent;
        _listener.PeerConnectedEvent += PeerConnected;
        _listener.PeerDisconnectedEvent += PeerDisconnected;
        _listener.NetworkReceiveEvent += NetworkReceiveEvent;
        _listener.NetworkReceiveUnconnectedEvent += (point, reader, type) =>
        {
            Plugin.Log.LogInfo($"Received data from {point.Address}:{point.Port}");
        };
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
        return _data.TryGetValue(id, out var data) ? data.Data : null;
    }
    
    public IEnumerable<PlayerNetworkData> GetAllPlayerData()
    {
        foreach (var pair in _data)
        {
            yield return pair.Value.Data;
        }
    }

    public void Send(IPacket packet, bool handleLocal = false, DeliveryMethod delivery = DeliveryMethod.ReliableOrdered, NetPeer except = null)
    {
        NetDataWriter writer = new();
        writer.Put((int)packet.TypeID());
        packet.Send(ref writer);
        if (except != null)
        {
            _netManager.SendToAll(writer, delivery, except);
        }
        else
        {
            _netManager.SendToAll(writer, delivery);
        }

        if (handleLocal)
        {
            PacketHandler.HandlePacket(packet);
        }
    }

    public void ReinstanciatePlayers()
    {
        if (PlayerManager.Instance != null)
        {
            foreach (var player in PlayerManager.Instance.RegisteredPlayers)
            {
                PlayerManager.UnregisterPlayer(player);
                Object.Destroy(player.gameObject);
            }
        }

        foreach (var pair in _data)
        {
            CreatePlayer(pair.Key);
        }
    }

    public void Local()
    {
        NatUtility.StopDiscovery();
        Plugin.Log.LogInfo($"Switching to Local");
        if (_netManager.IsRunning)
        {
            _netManager.Stop(true);
        }
        
        ClearData();
        Online = false;
        Server = true;
        LocalPlayer = -1;
        CreatePlayer(LocalPlayer);
    }

    private async void OnDeviceFound(object sender, DeviceEventArgs args)
    {
        var device = args.Device;
        Plugin.Log.LogInfo($"Device found {device.DeviceEndpoint.Address}:{device.DeviceEndpoint.Port}.");
        var mapping = new Mapping(Protocol.Udp, ActivePort, ActivePort, 7200, "ThronefallMP");
        try {
            await device.CreatePortMapAsync(mapping);
            var m = await device.GetSpecificMappingAsync(Protocol.Udp, ActivePort);
            Plugin.Log.LogInfo($"Mapping for port {ActivePort} created.");
        } catch (Exception e) {
            Plugin.Log.LogInfo($"Failed to create mapping {e}.");
        }
    }
    
    public void Host(int port)
    {
        NatUtility.StartDiscovery();
        Plugin.Log.LogInfo($"Hosting on {port}");
        ClearData();
        Online = true;
        Server = true;
        ActivePort = port;
        _netManager.Stop(true);
        _netManager.Start(port);
        _netManager.BroadcastReceiveEnabled = true;
        _netManager.IPv6Enabled = true;
        _netManager.UnconnectedMessagesEnabled = true;
        LocalPlayer = -1;
        CreatePlayer(LocalPlayer);
    }
    
    public void Connect(string address, int port, Action<ConnectionResponse> response = null)
    {
        NatUtility.StartDiscovery();
        Plugin.Log.LogInfo($"Attempting to connect to {address}:{port}");
        Online = true;
        Server = false;
        ActivePort = port;
        _connectionResponse = response;
        _netManager.Stop(true);
        _netManager.Start();
        _netManager.Connect(address, port, $"thronefall_mp_{PluginInfo.PLUGIN_VERSION}");
    }

    private void ClearData()
    {
        _latestLocalData = null;
        _data.Clear();
        foreach (var player in PlayerManager.Instance.RegisteredPlayers)
        {
            PlayerManager.UnregisterPlayer(player);
            Object.Destroy(player.gameObject);
        }
    }

    public void Update()
    {
        // if (Input.GetKeyDown(KeyCode.L))
        // {
        //     _netManager.SimulateLatency = !_netManager.SimulateLatency;
        //     _netManager.SimulationMinLatency = 100;
        //     _netManager.SimulationMaxLatency = 200;
        //     Plugin.Log.LogInfo($"Latency Simulation {_netManager.SimulateLatency}");
        // }
        // if (Input.GetKeyDown(KeyCode.K))
        // {
        //     _netManager.SimulatePacketLoss = !_netManager.SimulatePacketLoss;
        //     Plugin.Log.LogInfo($"Latency Simulation {_netManager.SimulatePacketLoss}");
        // }
        
        _netManager.PollEvents();
        if (_playerUpdateQueued)
        {
            var packet = new PlayerListPacket();
            Plugin.Log.LogInfo($"Sending player list");
            foreach (var pair in _data)
            {
                packet.Players.Add(new PlayerListPacket.PlayerData
                {
                    Id = pair.Key,
                    Position = pair.Value.Data.SharedData.Position
                });
                
                Plugin.Log.LogInfo($" {pair.Key} -> {pair.Value.Data.SharedData.Position}");
            }
            
            Send(packet);
            _playerUpdateQueued = false;
        }

        var playerData = GetPlayerData(LocalPlayer);
        if (playerData != null && playerData.SharedData != _latestLocalData)
        {
            var packet = new PlayerSyncPacket
            {
                PlayerID = LocalPlayer,
                Data = playerData.SharedData
            };
            Send(packet, delivery: DeliveryMethod.ReliableSequenced);
        }
    }

    private void ConnectionRequestEvent(ConnectionRequest request)
    {
        if (Server)
        {
            if (SceneTransitionManagerPatch.InLevelSelect)
            {
                request.AcceptIfKey($"thronefall_mp_{PluginInfo.PLUGIN_VERSION}");
            }
            else
            {
                request.Reject();
            }
        }
    }

    private void PeerConnected(NetPeer peer)
    {
        if (Server)
        {
            Plugin.Log.LogInfo($"Peer connected with id {peer.Id} and remote id {peer.RemoteId}");
            CreatePlayer(peer.Id);
            _data[peer.Id].Peer = peer;
            _playerUpdateQueued = true;
        }
        else
        {
            Online = true;
            Server = false;
            ClearData();
            LocalPlayer = peer.RemoteId;
            Plugin.Log.LogInfo($"Connected to server with peer id {LocalPlayer}");
        }

        if (_connectionResponse != null)
        {
            _connectionResponse(new ConnectionResponse()
            {
                Succeeded = true
            });
            _connectionResponse = null;
        }
    }
    
    private void PeerDisconnected(NetPeer peer, DisconnectInfo info)
    {
        Plugin.Log.LogInfo($"Peer disconnected with id {peer.Id} and remote id {peer.RemoteId}");
        if (_data.ContainsKey(peer.Id))
        {
            var player = _data[peer.Id].Data;
            _data.Remove(peer.Id);
            Object.Destroy(player.gameObject);
        }
        
        if (_connectionResponse != null)
        {
            _connectionResponse(new ConnectionResponse()
            {
                Succeeded = false,
                Reason = info.Reason
            });
            _connectionResponse = null;
        }

        if (!Server)
        {
            Plugin.Log.LogInfo($"Reason: {info.Reason}");
            Plugin.Log.LogInfo($"Error: {info.SocketErrorCode}");
            
            Local();
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
        if (!_data.ContainsKey(id))
        {
            _data.Add(id, new NetworkPeerData());
        }

        _data[id].Id = id;
        _data[id].Data = data;
        return newPlayer;
    }

    private void NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod delivery)
    {
        var type = (PacketId)reader.GetInt();
        var shouldPropagate = false;
        IPacket packet = null;
        switch (type)
        {
            case PlayerListPacket.PacketID:
            {
                packet = new PlayerListPacket();
                break;
            }
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
                    Plugin.Log.LogWarning($"Received unauthorized spawn packet from {peer.Id}.");
                    return;
                }
                
                packet = new EnemySpawnPacket();
                break;
            }
            case DamagePacket.PacketID:
            {
                if (Server)
                {
                    Plugin.Log.LogWarning($"Received unauthorized damage packet from {peer.Id}.");
                    return;
                }
                
                packet = new DamagePacket();
                break;
            }
            case HealPacket.PacketID:
            {
                if (Server)
                {
                    Plugin.Log.LogWarning($"Received unauthorized damage packet from {peer.Id}.");
                    return;
                }
                
                packet = new HealPacket();
                break;
            }
            case ScaleHpPacket.PacketID:
            {
                if (Server)
                {
                    Plugin.Log.LogWarning($"Received unauthorized damage packet from {peer.Id}.");
                    return;
                }
                
                packet = new ScaleHpPacket();
                break;
            }
            case PositionPacket.PacketID:
            {
                if (Server)
                {
                    Plugin.Log.LogWarning($"Received unauthorized position packet from {peer.Id}.");
                    return;
                }
                
                packet = new PositionPacket();
                break;
            }
            case RespawnPacket.PacketID:
            {
                if (Server)
                {
                    Plugin.Log.LogWarning($"Received unauthorized respawn packet from {peer.Id}.");
                    return;
                }
                
                packet = new RespawnPacket();
                break;
            }
            case PacketId.CommandAddPacket:
                if (Server)
                {
                    Plugin.Log.LogWarning($"Received unauthorized command add packet from {peer.Id}.");
                    return;
                }

                packet = new CommandAddPacket();
                break;
            case PacketId.CommandPlacePacket:
            {
                if (Server)
                {
                    Plugin.Log.LogWarning($"Received unauthorized command place packet from {peer.Id}.");
                    return;
                }

                packet = new CommandPlacePacket();
                break;
            }
            case PacketId.CommandHoldPositionPacket:
            {
                if (Server)
                {
                    Plugin.Log.LogWarning($"Received unauthorized command hold position packet from {peer.Id}.");
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
            default:
                Plugin.Log.LogWarning($"Received unknown packet {type} from {peer.Id} containing {reader.RawDataSize} bytes.");
                break;
        }

        if (packet == null)
        {
            return;
        }
        
        packet.Receive(ref reader);
        if (shouldPropagate)
        {
            Send(packet, false, delivery, peer);
        }
        
        PacketHandler.HandlePacket(packet);
    }
}