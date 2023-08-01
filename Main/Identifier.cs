using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ThronefallMP;

public enum IdentifierType
{
    Building,
    Enemy
}

public class Identifier : MonoBehaviour
{
    private static readonly Dictionary<IdentifierType, Dictionary<int, GameObject>> Repository = new()
    {
        { IdentifierType.Building, new Dictionary<int, GameObject>() },
        { IdentifierType.Enemy, new Dictionary<int, GameObject>() }
    };

    public IdentifierType Type { get; private set; }

    public int Id { get; private set; }

    public void SetIdentity(IdentifierType type, int id)
    {
        if (Id > 0)
        {
            // Only allow setting the id once.
            return;
        }
        
        Type = type;
        Id = id;
        if (Id >= 0)
        {
            Repository[Type].Add(Id, gameObject);
        }
    }

    public static GameObject GetGameObject(IdentifierType type, int id)
    {
        return Repository[type].GetValueSafe(id);
    }
}