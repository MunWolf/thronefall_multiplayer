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

    public void Send(IPacket packet, DeliveryMethod delivery = DeliveryMethod.ReliableOrdered, NetPeer except = null)
    {
        NetDataWriter writer = new();
        writer.Put(packet.TypeID());
        packet.Send(ref writer);
        if (except != null)
        {
            _netManager.SendToAll(writer, delivery, except);
        }
        else
        {
            _netManager.SendToAll(writer, delivery);
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
            createPlayer(pair.Key);
        }
    }

    public void Local()
    {
        Stop();
        
        Online = false;
        Server = true;
        LocalPlayer = -1;
        createPlayer(LocalPlayer);
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
            Send(packet, DeliveryMethod.ReliableSequenced);
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
            createPlayer(peer.Id);
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

    private void NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod delivery)
    {
        var type = reader.GetInt();
        switch (type)
        {
            case PlayerListPacket.PacketID:
            {
                var packet = new PlayerListPacket();
                packet.Receive(ref reader);
                Plugin.Log.LogInfo("Received player list");
                foreach (var data in packet.Players)
                {
                    if (!_data.ContainsKey(data.Id) || _data[data.Id].Data == null)
                    {
                        Plugin.Log.LogInfo($"Creating player {data.Id}");
                        createPlayer(data.Id);
                        var playerData = GetPlayerData(data.Id);
                        playerData.SharedData.Position = data.Position;
                        playerData.TeleportNext = true;
                    }
                    else
                    {
                        Plugin.Log.LogInfo($"Player {data.Id} exists");
                        GetPlayerData(data.Id).SharedData.Position = data.Position;
                    }
                }
                break;
            }
            case PlayerSyncPacket.PacketID:
            {
                var packet = new PlayerSyncPacket();
                packet.Receive(ref reader);
                if (Server)
                {
                    if (peer.Id != packet.PlayerID)
                    {
                        Plugin.Log.LogWarning($"Peer {peer.Id} send unauthorized packet for player {packet.PlayerID}");
                        return;
                    }
                    
                    Send(packet, DeliveryMethod.ReliableSequenced, except: peer);
                }

                if (_data.TryGetValue(packet.PlayerID, out var value))
                {
                    value.Data.SharedData = packet.Data;
                }
                break;
            }
            case TransitionToScenePacket.PacketID:
            {
                var packet = new TransitionToScenePacket();
                packet.Receive(ref reader);
                if (Server)
                {
                    Send(packet, except: peer);
                }

                SceneTransitionManagerPatch.DisableTransitionHook = true;
                SceneTransitionManager.instance.TransitionFromLevelSelectToLevel(packet.Level);
                SceneTransitionManagerPatch.DisableTransitionHook = false;
                break;
            }
            case BuildOrUpgradePacket.PacketID:
            {
                var packet = new BuildOrUpgradePacket();
                packet.Receive(ref reader);
                if (Server)
                {
                    Send(packet, except: peer);
                }
                
                BuildSlotPatch.HandleUpgrade(packet.BuildingId, packet.Level, packet.Choice);
                break;
            }
            case DayNightPacket.PacketID:
            {
                var packet = new DayNightPacket();
                packet.Receive(ref reader);
                if (Server)
                {
                    Send(packet, except: peer);
                }

                if (packet.Night)
                {
                    NightCallPatch.TriggerNightFall();
                }
                break;
            }
            case EnemySpawnPacket.PacketID:
            {
                if (Server)
                {
                    Plugin.Log.LogWarning($"Received unauthorized spawn packet from {peer.Id}.");
                    return;
                }
                
                var packet = new EnemySpawnPacket();
                packet.Receive(ref reader);
                EnemySpawnerPatch.SpawnEnemy(packet.Wave, packet.Spawn, packet.Position, packet.Id);
                break;
            }
            case DamagePacket.PacketID:
            {
                if (Server)
                {
                    Plugin.Log.LogWarning($"Received unauthorized damage packet from {peer.Id}.");
                    return;
                }
                
                var packet = new DamagePacket();
                packet.Receive(ref reader);
                HpPatch.InflictDamage(
                    packet.Target,
                    packet.Source,
                    packet.Damage,
                    packet.CausedByPlayer,
                    packet.InvokeFeedbackEvents
                );
                break;
            }
            case HealPacket.PacketID:
            {
                if (Server)
                {
                    Plugin.Log.LogWarning($"Received unauthorized damage packet from {peer.Id}.");
                    return;
                }
                
                var packet = new HealPacket();
                packet.Receive(ref reader);
                HpPatch.Heal(packet.Target, packet.Amount);
                break;
            }
            case ScaleHpPacket.PacketID:
            {
                if (Server)
                {
                    Plugin.Log.LogWarning($"Received unauthorized damage packet from {peer.Id}.");
                    return;
                }
                
                var packet = new ScaleHpPacket();
                packet.Receive(ref reader);
                HpPatch.ScaleHp(packet.Target, packet.Multiplier);
                break;
            }
            case PositionPacket.PacketID:
            {
                if (Server)
                {
                    Plugin.Log.LogWarning($"Received unauthorized position packet from {peer.Id}.");
                    return;
                }
                
                var packet = new PositionPacket();
                packet.Receive(ref reader);
                var target = Identifier.GetGameObject(packet.Target);
                if (target != null)
                {
                    target.transform.position = packet.Position;
                }
                break;
            }
            case RespawnPacket.PacketID:
            {
                if (Server)
                {
                    Plugin.Log.LogWarning($"Received unauthorized respawn packet from {peer.Id}.");
                    return;
                }
                
                var packet = new RespawnPacket();
                packet.Receive(ref reader);
                var target = Identifier.GetGameObject(packet.Target);
                if (target == null)
                {
                    return;
                }
                
                switch (packet.Target.Type)
                {
                    case IdentifierType.Ally:
                    {
                        var hp = target.GetComponent<Hp>();
                        UnitRespawnerForBuildingsPatch.RevivePlayerUnit(hp, packet.Position);
                        break;
                    }
                    case IdentifierType.Invalid:
                    case IdentifierType.Player:
                    case IdentifierType.Building:
                    case IdentifierType.Enemy:
                    default:
                        Plugin.Log.LogWarning($"Received unhandled respawn packet for {packet.Target.Type}:{packet.Target.Id}");
                        break;
                }
                target.transform.position = packet.Position;
                break;
            }
            default:
                Plugin.Log.LogWarning($"Received unknown packet {type} from {peer.Id} containing {reader.RawDataSize} bytes.");
                break;
        }
    }

    private GameObject createPlayer(int id)
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
}