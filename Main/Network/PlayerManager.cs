using System.Collections.Generic;
using System.Linq;
using Steamworks;
using ThronefallMP.Components;
using ThronefallMP.Patches;
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
            player = new Player
            {
                Id = id,
                Object = Object.Instantiate(_playerPrefab)
            };

            player.Controller = player.Object.GetComponent<CharacterController>();
            player.Data = player.Object.GetComponent<PlayerNetworkData>();
            player.Shared = player.Data.SharedData;
            player.Object.SetActive(true);
            player.Data.id = id;
            player.SpawnID = _players.Max(p => p.Value.SpawnID) + 1;
            var identifier = player.Object.GetComponent<Identifier>();
            identifier.SetIdentity(IdentifierType.Player, id);
        
            _players[id] = player;
        }

        player.Controller.enabled = false;
        player.Object.transform.position = Utils.GetSpawnLocation(PlayerMovementPatch.SpawnLocation, player.SpawnID);
        player.Data.SharedData.Position = player.Object.transform.position;
        player.Controller.enabled = true;
        return _players[id];
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
        
        _playerPrefab = Utils.InstantiateDisabled(prefab);
        var data = _playerPrefab.AddComponent<PlayerNetworkData>();
        data.id = -1;
        _playerPrefab.AddComponent<Identifier>();
        Plugin.Log.LogInfo("Initialized player prefab");
    }

    public void Clear()
    {
        foreach (var pair in _players)
        {
            Object.Destroy(pair.Value.Object);
        }
        
        _players.Clear();
    }

    public void ResetPlayersToSpawn()
    {
        foreach (var pair in _players)
        {
            pair.Value.Object.transform.position = Utils.GetSpawnLocation(
                PlayerMovementPatch.SpawnLocation,
                pair.Value.SpawnID
            );
        }
    }
}