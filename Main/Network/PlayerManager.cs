using System.Collections.Generic;
using System.Linq;
using Steamworks;
using ThronefallMP.Components;
using TMPro;
using UnityEngine;

namespace ThronefallMP.Network;

public class PlayerManager
{
    public class Player
    {
        public int Id;
        public CSteamID SteamID;
        public uint Ping;
        public int SpawnID;
        public GameObject Object;
        public PlayerNetworkData Data;
        public readonly PlayerNetworkData.Shared Shared = new();
        public CharacterController Controller;
        
        public Vector3 SpawnLocation => Helpers.GetSpawnLocation(Plugin.Instance.PlayerManager.SpawnLocation, SpawnID);
    }

    private int _localId;
    public int LocalId
    {
        get => _localId;
        set
        {
            _localId = value;
            Plugin.Log.LogInfoFiltered("Network", $"Local player set to {value}");
        }
    }

    public Player LocalPlayer => _players.TryGetValue(LocalId, out var value) ? value : null;
    
    private Dictionary<int, Player> _players = new();
    private Dictionary<CSteamID, Player> _steamToPlayer = new();
    private GameObject _playerContainer;
    private GameObject _playerPrefab;

    private Vector3 _spawn;

    public Vector3 SpawnLocation
    {
        get => _spawn;
        set
        {
            _spawn = value;
            foreach (var pair in _players)
            {
                if (pair.Value.Object != null)
                {
                    pair.Value.Object.transform.position = Helpers.GetSpawnLocation(
                        _spawn,
                        pair.Value.SpawnID
                    );
                }
            }
        }
    }
    
    public int GenerateID()
    {
        int id;
        do { id = Random.Range(0, int.MaxValue); }
        while (_players.ContainsKey(id));
        return id;
    }
    
    public Player CreateOrGet(CSteamID steamId, int id)
    {
        if (!_players.TryGetValue(id, out var player))
        {
            Plugin.Log.LogInfo($"Creating player {steamId}:{id}");
            player = new Player
            {
                Id = id,
                SteamID = steamId
            };
        
            _players[id] = player;
            _steamToPlayer[steamId] = player;
        }
        else
        {
            Plugin.Log.LogInfo($"Updating Player {steamId}:{id}");
        }

        return player;
    }

    public void Remove(int player)
    {
        // TODO: Add a smoke poof effect when a player is destroyed.
        var data = _players[player];
        Plugin.Log.LogInfo($"Destroying player {data.SteamID}:{player}");
        Object.Destroy(data.Object);
        _steamToPlayer.Remove(data.SteamID);
        _players.Remove(player);
        
        // TODO: Update spawn ids.
    }

    public bool InstantiatePlayer(Player player, Vector3 position)
    {
        if (_playerPrefab == null)
        {
            return false;
        }

        if (player.Object != null)
        {
            Object.Destroy(player.Object);
        }
        
        Plugin.Log.LogInfo($"Instantiating player {player.SteamID}:{player.Id} at {Helpers.GetSpawnLocation(_spawn, player.SpawnID)}");
        player.Object = Object.Instantiate(_playerPrefab, _playerContainer.transform);
        player.Controller = player.Object.GetComponent<CharacterController>();
        player.Data = player.Object.GetComponent<PlayerNetworkData>();
        player.Data.Player = player;
        player.Data.SharedData = player.Shared;
        player.Data.id = player.Id;
        
        var identifier = player.Object.GetComponent<Identifier>();
        identifier.SetIdentity(IdentifierType.Player, player.Id);
        player.Object.transform.position = position;
        player.Object.SetActive(true);

        var label = new GameObject($"player_{player.Id}_label");
        label.SetActive(false);
        label.AddComponent<TextMeshPro>();
        var follower = label.AddComponent<PlayerLabelFollower>();
        follower.TargetPlayer = player.Id;
        label.SetActive(true);
        return true;
    }

    public Player Get(int id)
    {
        return _players.TryGetValue(id, out var player) ? player : null;
    }

    public Player Get(CSteamID id)
    {
        return _steamToPlayer.TryGetValue(id, out var player) ? player : null;
    }

    public IEnumerable<Player> GetAllPlayers()
    {
        return _players.Select(p => p.Value);
    }

    public IEnumerable<PlayerNetworkData> GetAllPlayerData()
    {
        return _players.Select(p => p.Value.Data);
    }

    public void SetPrefab(GameObject prefab)
    {
        if (_playerPrefab != null)
        {
            Object.Destroy(_playerPrefab);
            _playerPrefab = null;
        }

        if (prefab == null)
        {
            return;
        }
        
        _playerContainer = prefab.transform.parent.gameObject;
        _playerPrefab = Helpers.InstantiateDisabled(prefab, Plugin.Instance.transform, worldPositionStays: true);
        
        var data = _playerPrefab.AddComponent<PlayerNetworkData>();
        data.id = -1;
        _playerPrefab.AddComponent<Identifier>();
        
        
        Plugin.Log.LogInfo("Initialized player prefab");

        SpawnLocation = prefab.transform.position;
        foreach (var player in _players)
        {
            InstantiatePlayer(player.Value, player.Value.SpawnLocation);
        }
    }

    public void Clear()
    {
        foreach (var pair in _players)
        {
            Object.Destroy(pair.Value.Object);
        }
        
        _players.Clear();
        _steamToPlayer.Clear();
    }
}