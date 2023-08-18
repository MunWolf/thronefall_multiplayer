using System.Collections.Generic;
using On.NGS.MeshFusionPro;
using ThronefallMP.Components;
using ThronefallMP.Utils;
using UnityEngine;

namespace ThronefallMP;

public class PrefabSpawner : MonoBehaviour
{
    public enum EnemyType
    {
        Unknown,
        Archer,
        Catapult,
        Crossbow,
        Exploder,
        Flyer,
        Hunterling,
        Melee,
        MonsterRider,
        Ogre,
        ProjectileBow,
        Racer,
        Slime,
        StrongSlime,
        Weakling
    }

    private readonly Dictionary<string, EnemyType> _enemyNameToType = new();
    private readonly Dictionary<EnemyType, string> _enemyTypeToName = new();
    private readonly Dictionary<EnemyType, GameObject> _prefabs = new();

    private void Awake()
    {
        AddEnemyEntries(EnemyType.Unknown, "");
        AddEnemyEntries(EnemyType.Archer, "E Archer");
        AddEnemyEntries(EnemyType.Catapult, "E Catapult");
        AddEnemyEntries(EnemyType.Crossbow, "E Crossbow");
        AddEnemyEntries(EnemyType.Exploder, "E Exploder");
        AddEnemyEntries(EnemyType.Flyer, "E Flyer");
        AddEnemyEntries(EnemyType.Hunterling, "E Hunterling");
        AddEnemyEntries(EnemyType.Melee, "E Melee");
        AddEnemyEntries(EnemyType.MonsterRider, "E MonsterRider");
        AddEnemyEntries(EnemyType.Ogre, "E Ogre");
        AddEnemyEntries(EnemyType.ProjectileBow, "E ProjectileBow");
        AddEnemyEntries(EnemyType.Racer, "E Racer");
        AddEnemyEntries(EnemyType.Slime, "E Slime");
        AddEnemyEntries(EnemyType.StrongSlime, "E StrongSlime");
        AddEnemyEntries(EnemyType.Weakling, "E Weakling");
    }

    private void AddEnemyEntries(EnemyType type, string enemy)
    {
        _enemyTypeToName.Add(type, enemy);
        _enemyNameToType.Add(enemy, type);
    }

    private GameObject GetPrefab(EnemyType type)
    {
        if (!_prefabs.TryGetValue(type, out var enemy) || enemy == null)
        {
            enemy = GameObject.Find(_enemyTypeToName[type]);
            _prefabs.Add(type, enemy);
        }

        return enemy;
    }
    
    public EnemyType ToEnemy(string enemy)
    {
        return _enemyNameToType.TryGetValue(enemy, out var type) ? type : EnemyType.Unknown;
    }
    
    public GameObject Spawn(ushort id, EnemyType type)
    {
        var prefab = GetPrefab(type);
        if (prefab == null)
        {
            return null;
        }

        var enemy = Helpers.InstantiateDisabled(prefab);
        var idComponent = enemy.AddComponent<Identifier>();
        idComponent.SetIdentity(IdentifierType.Enemy, id);
        return enemy;
    }
}