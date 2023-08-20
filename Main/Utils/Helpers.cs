using UnityEngine;

namespace ThronefallMP.Utils;

public static class Helpers
{
    public const float Epsilon = 128 * float.Epsilon;
    public const float EpsilonSqr = 64 * float.Epsilon;
    
    private const float SpawnRadiusIncrement = 5.0f;
    private const int SpawnAngleIncrement = 60;
    private const int SpawnAngleEvenOffset = 30;
    
    public static Vector3 GetSpawnLocation(Vector3 position, int playerId)
    {
        if (playerId == -1)
        {
            return position;
        }

        var currentRadius = SpawnRadiusIncrement;
        var currentAngle = 0;
        var useOffset = false;
        while (playerId > 0)
        {
            currentAngle += SpawnAngleIncrement;
            if (currentAngle >= 360)
            {
                currentAngle = 0;
                currentRadius += SpawnRadiusIncrement;
                useOffset = !useOffset;
            }
            --playerId;
        }

        var offset = new Vector3 { x = currentRadius };
        offset = Quaternion.AngleAxis(currentAngle + (useOffset ? SpawnAngleEvenOffset : 0), Vector3.up) * offset;
        return position + offset;
    }
    
    public static string GetPath(Transform tr)
    {
        Transform parent = tr.parent;
        return parent == null ? tr.name : GetPath(parent) + "/" + tr.name;
    }

    public static bool UnityNullCheck(object a)
    {
        return a != null && (!(a is UnityEngine.Object o) || o != null);
    }

    public static PlayerInteraction FindClosest(PlayerInteraction[] players, Vector3 pos)
    {
        var distance = float.MaxValue;
        PlayerInteraction closest = null;
        foreach (var player in players)
        {
            var playerDistance = (player.transform.position - pos).sqrMagnitude;
            if (playerDistance < distance)
            {
                distance = playerDistance;
                closest = player;
            }
        }

        return closest;
    }
    
    /// <summary>
    /// Will instantiate an object disabled preventing it from calling Awake/OnEnable.
    /// </summary>
    public static T InstantiateDisabled<T>(T original, Transform parent = null, bool worldPositionStays = false) where T : Object
    {
        if (!GetActiveState(original))
        {
            return Object.Instantiate(original, parent, worldPositionStays);
        }
		
        var (coreObject, coreObjectTransform) = CreateDisabledCoreObject(parent);
        var instance = Object.Instantiate(original, coreObjectTransform, worldPositionStays);
        SetActiveState(instance, false);
        SetParent(instance, parent, worldPositionStays);
        Object.Destroy(coreObject);
        return instance;
    }
    
    /// <summary>
    /// Will instantiate an object disabled preventing it from calling Awake/OnEnable.
    /// </summary>
    public static T InstantiateDisabled<T>(T original, Vector3 position, Quaternion rotation, Transform parent = null) where T : Object
    {
        if (!GetActiveState(original))
        {
            return Object.Instantiate(original, position, rotation, parent);
        }
		
        (GameObject coreObject, Transform coreObjectTransform) = CreateDisabledCoreObject(parent);
        T instance = Object.Instantiate(original, position, rotation, coreObjectTransform);
        SetActiveState(instance, false);
        SetParent(instance, parent, false);
        Object.Destroy(coreObject);
        return instance;
    }
	
    private static (GameObject coreObject, Transform coreObjectTransform) CreateDisabledCoreObject(Transform parent = null)
    {
        GameObject coreObject = new GameObject(string.Empty);
        coreObject.SetActive(false);
        Transform coreObjectTransform = coreObject.transform;
        coreObjectTransform.SetParent(parent);

        return (coreObject, coreObjectTransform);
    }

    private static bool GetActiveState<T>(T @object) where T : Object
    {
        switch (@object)
        {
            case GameObject gameObject:
            {
                return gameObject.activeSelf;
            }
            case Component component:
            {
                return component.gameObject.activeSelf;
            }
            default:
            {
                return false;
            }
        }
    }

    private static void SetActiveState<T>(T @object, bool state) where T : Object
    {
        switch (@object)
        {
            case GameObject gameObject:
            {
                gameObject.SetActive(state);

                break;
            }
            case Component component:
            {
                component.gameObject.SetActive(state);

                break;
            }
        }
    }

    private static void SetParent<T>(T @object, Transform parent, bool worldPositionStays) where T : Object
    {
        switch (@object)
        {
            case GameObject gameObject:
            {
                gameObject.transform.SetParent(parent, worldPositionStays);

                break;
            }
            case Component component:
            {
                component.transform.SetParent(parent, worldPositionStays);

                break;
            }
        }
    }
}