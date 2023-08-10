using System.Collections.Generic;
using System.Linq;
using Steamworks;
using ThronefallMP.Components;
using UnityEngine;

namespace ThronefallMP.Network;

public class PlayerManager
{
    public class Player
    {
        public int Id;
        public CSteamID SteamID;
        public int SpawnID;
        public GameObject Object;
        public PlayerNetworkData Data;
        public PlayerNetworkData.Shared Shared;
        public CharacterController Controller;
    }

    public int LocalId { get; set; }
    public Player LocalPlayer => _players.TryGetValue(LocalId, out var value) ? value : null;
    
    private Dictionary<int, Player> _players = new();
    private GameObject _playerPrefab;

    private HashSet<int> _queuedPlayerCreation = new();

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
                    pair.Value.Object.transform.position = Utils.GetSpawnLocation(
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
    
    public Player Create(int id)
    {
        if (!_players.TryGetValue(id, out var player))
        {
            Plugin.Log.LogInfo($"Creating player {id}");
            player = new Player
            {
                Id = id
            };
        
            _players[id] = player;
        }

        if (_playerPrefab == null)
        {
            _queuedPlayerCreation.Add(id);
        }
        else if (player.Object == null)
        {
            InstantiatePlayer(player);
        }

        return player;
    }

    private void InstantiatePlayer(Player player)
    {
        player.Object = Object.Instantiate(_playerPrefab, _playerPrefab.transform.parent);
        player.Controller = player.Object.GetComponent<CharacterController>();
        player.Data = player.Object.GetComponent<PlayerNetworkData>();
        player.Shared = player.Data.SharedData;
        player.Data.id = player.Id;
        player.SpawnID = !_players.Any() ? 0 : _players.Max(p => p.Value.SpawnID) + 1;
        player.Object.SetActive(true);
        
        var identifier = player.Object.GetComponent<Identifier>();
        identifier.SetIdentity(IdentifierType.Player, player.Id);
        player.Controller.enabled = false;
        player.Object.transform.position = Utils.GetSpawnLocation(_spawn, player.SpawnID);
        player.Data.SharedData.Position = player.Object.transform.position;
        player.Controller.enabled = true;
    }

    public Player Get(int id)
    {
        return _players[id];
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
        }
        
        _playerPrefab = Utils.InstantiateDisabled(prefab, prefab.transform.parent, worldPositionStays: true);
        var data = _playerPrefab.AddComponent<PlayerNetworkData>();
        data.id = -1;
        _playerPrefab.AddComponent<Identifier>();
        Plugin.Log.LogInfo("Initialized player prefab");

        SpawnLocation = prefab.transform.position;
        while (_queuedPlayerCreation.Count > 0)
        {
            var item = _queuedPlayerCreation.First();
            _queuedPlayerCreation.Remove(item);
            InstantiatePlayer(_players[item]);
        }
    }

    public void Clear()
    {
        foreach (var pair in _players)
        {
            Object.Destroy(pair.Value.Object);
        }
        
        _players.Clear();
    }
}