using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using HarmonyLib;
using UnityEngine;

namespace ThronefallMP.Components;

public enum IdentifierType
{
    Invalid,
    Player,
    BuildSlot,
    Building,
    Ally,
    Enemy
}

public struct IdentifierData
{
    public static readonly IdentifierData Invalid = new() { Type = IdentifierType.Invalid, Id = -1 };
    
    public IdentifierType Type;
    public int Id;

    public IdentifierData(Identifier identity)
    {
        if (identity != null)
        {
            Type = identity.Type;
            Id = identity.Id;
        }
    }

    [Pure]
    public GameObject Get()
    {
        return Identifier.GetGameObject(Type, Id);
    }
}

public class Identifier : MonoBehaviour
{
    private static readonly Dictionary<IdentifierType, HashSet<int>> DestroyedRepository = new()
    {
        { IdentifierType.Player, new HashSet<int>() },
        { IdentifierType.BuildSlot, new HashSet<int>() },
        { IdentifierType.Building, new HashSet<int>() },
        { IdentifierType.Ally, new HashSet<int>() },
        { IdentifierType.Enemy, new HashSet<int>() }
    };
    
    private static readonly Dictionary<IdentifierType, Dictionary<int, GameObject>> Repository = new()
    {
        { IdentifierType.Player, new Dictionary<int, GameObject>() },
        { IdentifierType.BuildSlot, new Dictionary<int, GameObject>() },
        { IdentifierType.Building, new Dictionary<int, GameObject>() },
        { IdentifierType.Ally, new Dictionary<int, GameObject>() },
        { IdentifierType.Enemy, new Dictionary<int, GameObject>() }
    };

    public IdentifierType Type { get; private set; }

    public int Id { get; private set; }

    public void OnDisable()
    {
        if (Type == IdentifierType.Player && PlayerManager.Instance != null)
        {
            PlayerManager.UnregisterPlayer(GetComponent<PlayerMovement>());
        }
    }

    public void OnDestroy()
    {
        Repository[Type].Remove(Id);
        DestroyedRepository[Type].Add(Id);
    }

    public void SetIdentity(IdentifierType type, int id)
    {
        if (Repository[type].ContainsKey(id))
        {
            if (Repository[type][id].GetInstanceID() == gameObject.GetInstanceID())
            {
                return;
            }
            
            Plugin.Log.LogWarning($"Identifier {type}:{id} already registered");
            Plugin.Log.LogInfo($"{Helpers.GetPath(Repository[type][id].transform)}");
            Plugin.Log.LogInfo($"{Helpers.GetPath(transform)}");
        }

        if (DestroyedRepository[type].Contains(id))
        {
            DestroyedRepository[type].Remove(id);
        }
        
        Type = type;
        Id = id;
        Repository[Type][Id] = gameObject;
    }

    public static void Clear(IdentifierType type)
    {
        Repository[type].Clear();
    }
    
    public static GameObject GetGameObject(IdentifierType type, int id)
    {
        return type == IdentifierType.Invalid ? null : Repository[type].GetValueSafe(id);
    }
    
    public static IEnumerable<(int id, GameObject target)> GetIdentifiers(IdentifierType type)
    {
        foreach (var pair in Repository[type])
        {
            if (pair.Value != null)
            {
                yield return (pair.Key, pair.Value);
            }
        }
    }
    
    public static IEnumerable<GameObject> GetGameObjects(IdentifierType type)
    {
        foreach (var pair in Repository[type])
        {
            if (pair.Value != null)
            {
                yield return pair.Value;
            }
        }
    }
    
    public static IEnumerable<int> GetDestroyed(IdentifierType type)
    {
        foreach (var id in DestroyedRepository[type])
        {
            yield return id;
        }
    }
    
    public static bool WasDestroyed(IdentifierType type, int id)
    {
        return DestroyedRepository[type].Contains(id);
    }

    public static void ClearDestroyed()
    {
        foreach (var pair in DestroyedRepository)
        {
            pair.Value.Clear();
        }
    }
}