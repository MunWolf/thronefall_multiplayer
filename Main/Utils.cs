using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace ThronefallMP;

public static class Utils
{
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

    public static void Put(this NetDataWriter writer, Vector3 vec)
    {
        writer.Put(vec.x);
        writer.Put(vec.y);
        writer.Put(vec.z);
    }

    public static Vector3 GetVector3(this NetPacketReader reader)
    {
        return new Vector3(
            reader.GetFloat(),
            reader.GetFloat(),
            reader.GetFloat()
        );
    }

    public static void Put(this NetDataWriter writer, IdentifierData id)
    {
        writer.Put((int)id.Type);
        writer.Put(id.Id);
    }

    public static IdentifierData GetIdentifierData(this NetPacketReader reader)
    {
        return new IdentifierData
        {
            Type = (IdentifierType)reader.GetInt(),
            Id = reader.GetInt(),
        };
    }
}