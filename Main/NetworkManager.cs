using System.Collections.Generic;
using System.Linq;
using LiteNetLib;
using LiteNetLib.Utils;
using Rewired;
using ThronefallMP.NetworkPackets;
using ThronefallMP.Patches;
using UnityEngine;

namespace ThronefallMP;

public class NetworkManager
{
    public class NetworkPeerData
    {
        public int Id;
        public NetPeer Peer = null;
        public PlayerNetworkData Data = null;
    }
    
    public int LocalPlayer { get; private set; } = -1;
    public PlayerNetworkData LocalPlayerData { get { return GetPlayerData(LocalPlayer); } }
    public bool Online { get; private set; } = false;
    public bool Server { get; private set; } = true;
    public int ActivePort { get; private set; } = 0;

    private readonly EventBasedNetListener _listener;
    private readonly NetManager _netManager;
    private readonly Dictionary<int, NetworkPeerData> _data = new();
    
    private GameObject _playerPrefab = null;
    private bool _playerUpdateQueued = false;
    private PlayerNetworkData.Shared? _latestLocalData = null;

    public NetworkManager()
    {
        _listener = new EventBasedNetListener();
        _netManager = new NetManager(_listener);
        _listener.ConnectionRequestEvent += ConnectionRequestEvent;
        _listener.PeerConnectedEvent += PeerConnected;
        _listener.PeerDisconnectedEvent += PeerDisconnected;
        _listener.NetworkReceiveEvent += NetworkReceiveEvent;
    }

    public void InitializeDefaultPlayer(GameObject player)
    {
        if (_playerPrefab != null)
        {
            Object.Destroy(_playerPrefab);
        }
        
        player.SetActive(false);
        _playerPrefab = Object.Instantiate(player, null, true);
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
        Stop();
        
        Online = false;
        Server = true;
        LocalPlayer = -1;
        CreatePlayer(LocalPlayer);
    }

    public void Host(int port)
    {
        Stop();
        
        Online = true;
        Server = true;
        ActivePort = port;
        _netManager.Start(port);
        LocalPlayer = -1;
        _data.Add(LocalPlayer, new NetworkPeerData());
        _data[LocalPlayer].Id = LocalPlayer;
        
        var newPlayer = Object.Instantiate(_playerPrefab);
        newPlayer.SetActive(true);
        newPlayer.transform.position = Utils.GetSpawnLocation(PlayerMovementPatch.SpawnLocation, LocalPlayer);
        var data = newPlayer.GetComponent<PlayerNetworkData>();
        data.id = LocalPlayer;
        _data[LocalPlayer].Data = data;
    }
    
    public void Connect(string address, int port)
    {
        Stop();
        Online = true;
        Server = false;
        ActivePort = port;
        _netManager.Start();
        var peer = _netManager.Connect(address, port, "test");
        _data.Clear();
    }

    public void Stop()
    {
        Online = false;
        Server = true;
        _netManager.Stop(true);
        _latestLocalData = null;
        _data.Clear();
        foreach (var player in PlayerManager.Instance.RegisteredPlayers)
        {;
            PlayerManager.UnregisterPlayer(player);
            Object.Destroy(player.gameObject);
        }
    }

    public void Update()
    {
        _netManager.PollEvents();
        if (_playerUpdateQueued)
        {
            var packet = new PlayerListPacket();
            foreach (var pair in _data)
            {
                packet.Players.Add(new PlayerListPacket.PlayerData
                {
                    Id = pair.Key,
                    Position = pair.Value.Data.SharedData.Position
                });
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
            request.Accept();
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
            Plugin.Log.LogInfo("Local " + LocalPlayer);
            foreach (var pair in _data)
            {
                Plugin.Log.LogInfo($"peer data exists for {pair.Key}");
            }
        }
        else
        {
            LocalPlayer = peer.RemoteId;
            Plugin.Log.LogInfo($"Connected to server with peer id {LocalPlayer}");
        }
    }

    private void PeerDisconnected(NetPeer peer, DisconnectInfo info)
    {
        Plugin.Log.LogInfo($"Peer disconnected with id {peer.Id} and remote id {peer.RemoteId}");
        var player = _data[peer.Id].Data;
        _data.Remove(peer.Id);
        Object.Destroy(player.gameObject);
    }

    public GameObject CreatePlayer(int id)
    {
        var newPlayer = Object.Instantiate(_playerPrefab);
        newPlayer.SetActive(true);
        newPlayer.transform.position = Utils.GetSpawnLocation(PlayerMovementPatch.SpawnLocation, id);
        var data = newPlayer.GetComponent<PlayerNetworkData>();
        data.id = id;
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