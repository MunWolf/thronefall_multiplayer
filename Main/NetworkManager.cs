using System.Collections.Generic;
using System.Linq;
using LiteNetLib;
using LiteNetLib.Utils;
using Rewired;
using ThronefallMP.NetworkPackets;
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
    public bool Online { get; private set; } = false;
    public bool Server { get; private set; } = true;
    public int ActivePort { get; private set; } = 0;

    private readonly EventBasedNetListener _listener;
    private readonly NetManager _netManager;
    private readonly Dictionary<int, NetworkPeerData> _data = new();
    
    private GameObject _playerPrefab = null;
    private bool _playerPrefabInitialized = false;
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
        _data.Add(LocalPlayer, new NetworkPeerData());
        _data[LocalPlayer].Id = LocalPlayer;
        var data = player.GetComponent<PlayerNetworkData>();
        data.id = -1;
        _data[LocalPlayer].Data = data;
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

    public void Send(IPacket packet, NetPeer except = null)
    {
        NetDataWriter writer = new();
        writer.Put(packet.TypeID());
        packet.Send(ref writer);
        if (except != null)
        {
            _netManager.SendToAll(writer, DeliveryMethod.ReliableSequenced, except);
        }
        else
        {
            _netManager.SendToAll(writer, DeliveryMethod.ReliableSequenced);
        }
    }

    public void ReinstanciatePlayers()
    {
        foreach (var player in PlayerManager.Instance.RegisteredPlayers)
        {
            PlayerManager.UnregisterPlayer(player);
            Object.Destroy(player.gameObject);
        }

        foreach (var pair in _data)
        {
            var newPlayer = Object.Instantiate(_playerPrefab);
            newPlayer.SetActive(true);
            var data = newPlayer.GetComponent<PlayerNetworkData>();
            data.id = pair.Key;
            pair.Value.Data = data;
        }
    }

    public void Local()
    {
        Stop();
        
        Online = false;
        Server = true;
        LocalPlayer = -1;
        _data.Add(LocalPlayer, new NetworkPeerData());
        _data[LocalPlayer].Id = LocalPlayer;
        
        var newPlayer = Object.Instantiate(_playerPrefab);
        newPlayer.SetActive(true);
        var data = newPlayer.GetComponent<PlayerNetworkData>();
        data.id = -1;
        _data[LocalPlayer].Data = data;
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
        var data = newPlayer.GetComponent<PlayerNetworkData>();
        data.id = -1;
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
        {
            Plugin.Log.LogInfo("Removing player");
            PlayerManager.UnregisterPlayer(player);
            Object.Destroy(player.gameObject);
        }
    }

    public void Update()
    {
        if (!_playerPrefabInitialized && PlayerMovement.instance != null)
        {
            _playerPrefab = Object.Instantiate(PlayerMovement.instance.gameObject);
            _playerPrefab.SetActive(false);
            _playerPrefabInitialized = true;
        }
        
        _netManager.PollEvents();
        if (_playerUpdateQueued)
        {
            var packet = new PlayerListPacket { PlayerIDs = _data.Keys.ToList() };
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
            Send(packet);
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
            Plugin.Log.LogInfo("Peer connected with id " + peer.Id + " and remote id " + peer.RemoteId);
            var newPlayer = Object.Instantiate(_playerPrefab);
            newPlayer.SetActive(true);
            var data = newPlayer.GetComponent<PlayerNetworkData>();
            var id = peer.Id;
            _data.Add(id, new NetworkPeerData());
            _data[id].Peer = peer;
            _data[id].Data = data;
            _playerUpdateQueued = true;
            Plugin.Log.LogInfo("Local " + LocalPlayer);
            foreach (var pair in _data)
            {
                Plugin.Log.LogInfo("peer data exists for " + pair.Key);
            }
        }
        else
        {
            LocalPlayer = peer.RemoteId;
            Plugin.Log.LogInfo("Connected to server with peer id " + LocalPlayer);
        }
    }

    private void PeerDisconnected(NetPeer peer, DisconnectInfo info)
    {
        Plugin.Log.LogInfo("Peer disconnected with id " + peer.Id + " and remote id " + peer.RemoteId);
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
                foreach (var id in packet.PlayerIDs)
                {
                    if (!_data.ContainsKey(id))
                    {
                        _data.Add(id, new NetworkPeerData());
                        _data[id].Id = id;
        
                        var newPlayer = Object.Instantiate(_playerPrefab);
                        newPlayer.SetActive(true);
                        var data = newPlayer.GetComponent<PlayerNetworkData>();
                        data.id = id;
                        _data[id].Data = data;
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
                        Plugin.Log.LogWarning("Peer " + peer.Id + " send unauthorized packet for player " + packet.PlayerID);
                        return;
                    }
                    
                    Send(packet, peer);
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
                    Send(packet, peer);
                }

                SceneTransitionManagerPatch.DisableTransitionHook = true;
                SceneTransitionManager.instance.TransitionFromLevelSelectToLevel(packet.Level);
                SceneTransitionManagerPatch.DisableTransitionHook = false;
                break;
            }
        }
    }
}