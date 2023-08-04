using LiteNetLib;
using LiteNetLib.Utils;
using ThronefallMP.Components;
using UnityEngine;

namespace ThronefallMP;

public static class Ext
{
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
    
    public static void Put(this NetDataWriter writer, Quaternion quat)
    {
        writer.Put(quat.x);
        writer.Put(quat.y);
        writer.Put(quat.z);
        writer.Put(quat.w);
    }

    public static Quaternion GetQuaternion(this NetPacketReader reader)
    {
        return new Quaternion(
            reader.GetFloat(),
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