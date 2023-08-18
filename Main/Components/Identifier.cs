using System.Collections.Generic;
using System.Diagnostics.Contracts;
using HarmonyLib;
using ThronefallMP.Utils;
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
    public static readonly IdentifierData Invalid = new() { Type = IdentifierType.Invalid, Id = 0 };
    
    public IdentifierType Type;
    public ushort Id;

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
    private static readonly Dictionary<IdentifierType, HashSet<ushort>> DestroyedRepository = new()
    {
        { IdentifierType.Player, new HashSet<ushort>() },
        { IdentifierType.BuildSlot, new HashSet<ushort>() },
        { IdentifierType.Building, new HashSet<ushort>() },
        { IdentifierType.Ally, new HashSet<ushort>() },
        { IdentifierType.Enemy, new HashSet<ushort>() }
    };
    
    private static readonly Dictionary<IdentifierType, Dictionary<ushort, GameObject>> Repository = new()
    {
        { IdentifierType.Player, new Dictionary<ushort, GameObject>() },
        { IdentifierType.BuildSlot, new Dictionary<ushort, GameObject>() },
        { IdentifierType.Building, new Dictionary<ushort, GameObject>() },
        { IdentifierType.Ally, new Dictionary<ushort, GameObject>() },
        { IdentifierType.Enemy, new Dictionary<ushort, GameObject>() }
    };

    public IdentifierType Type { get; private set; }

    public ushort Id { get; private set; }

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

    public void SetIdentity(IdentifierType type, ushort id)
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
    
    public static GameObject GetGameObject(IdentifierType type, ushort id)
    {
        return type == IdentifierType.Invalid ? null : Repository[type].GetValueSafe(id);
    }
    
    public static IEnumerable<(ushort id, GameObject target)> GetIdentifiers(IdentifierType type)
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
    
    public static IEnumerable<ushort> GetDestroyed(IdentifierType type)
    {
        foreach (var id in DestroyedRepository[type])
        {
            yield return id;
        }
    }
    
    public static bool WasDestroyed(IdentifierType type, ushort id)
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