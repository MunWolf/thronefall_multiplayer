using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ThronefallMP;

public enum IdentifierType
{
    Invalid,
    Player,
    Building,
    Ally,
    Enemy
}

public struct IdentifierData
{
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
}

public class Identifier : MonoBehaviour
{
    private static readonly Dictionary<IdentifierType, Dictionary<int, GameObject>> Repository = new()
    {
        { IdentifierType.Player, new Dictionary<int, GameObject>() },
        { IdentifierType.Building, new Dictionary<int, GameObject>() },
        { IdentifierType.Ally, new Dictionary<int, GameObject>() },
        { IdentifierType.Enemy, new Dictionary<int, GameObject>() }
    };

    public IdentifierType Type { get; private set; }

    public int Id { get; private set; }

    public void SetIdentity(IdentifierType type, int id)
    {
        Type = type;
        Id = id;
        Repository[Type][Id] = gameObject;
        Plugin.Log.LogInfo($"Added {type}:{id} to identifier repository.");
    }

    public static GameObject GetGameObject(IdentifierType type, int id)
    {
        return type == IdentifierType.Invalid ? null : Repository[type].GetValueSafe(id);
    }

    public static GameObject GetGameObject(IdentifierData data)
    {
        return GetGameObject(data.Type, data.Id);
    }
}