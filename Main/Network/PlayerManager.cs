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
        public readonly PlayerNetworkData.Shared Shared = new();
        public CharacterController Controller;
    }

    public int LocalId { get; set; }
    public Player LocalPlayer => _players.TryGetValue(LocalId, out var value) ? value : null;
    
    private Dictionary<int, Player> _players = new();
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
            Plugin.Log.LogInfo($"Creating player {id} (local {LocalId})");
            player = new Player
            {
                Id = id
            };
        
            _players[id] = player;
        }

        if (_playerPrefab != null && player.Object == null)
        {
            InstantiatePlayer(player);
        }

        return player;
    }

    private void InstantiatePlayer(Player player)
    {
        Plugin.Log.LogInfo($"Instantiating player '{player.Id} at {Utils.GetSpawnLocation(_spawn, player.SpawnID)}'");
        player.Object = Object.Instantiate(_playerPrefab, _playerContainer.transform);
        player.Controller = player.Object.GetComponent<CharacterController>();
        player.Data = player.Object.GetComponent<PlayerNetworkData>();
        player.Data.Player = player;
        player.Data.SharedData = player.Shared;
        player.Data.id = player.Id;
        player.SpawnID = !_players.Any() ? 0 : _players.Max(p => p.Value.SpawnID) + 1;
        
        var identifier = player.Object.GetComponent<Identifier>();
        identifier.SetIdentity(IdentifierType.Player, player.Id);
        player.Object.transform.position = Utils.GetSpawnLocation(_spawn, player.SpawnID);
        player.Data.SharedData.Position = player.Object.transform.position;
        player.Object.SetActive(true);
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

        _playerContainer = prefab.transform.parent.gameObject;
        _playerPrefab = Utils.InstantiateDisabled(prefab, Plugin.Instance.transform, worldPositionStays: true);
        
        var data = _playerPrefab.AddComponent<PlayerNetworkData>();
        data.id = -1;
        _playerPrefab.AddComponent<Identifier>();
        Plugin.Log.LogInfo("Initialized player prefab");

        SpawnLocation = prefab.transform.position;
        foreach (var player in _players)
        {
            InstantiatePlayer(player.Value);
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