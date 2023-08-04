using Lidgren.Network;
using ThronefallMP.Components;
using UnityEngine;

namespace ThronefallMP;

public static class Ext
{
    public static void Write(this NetBuffer writer, Vector3 vec)
    {
        writer.Write(vec.x);
        writer.Write(vec.y);
        writer.Write(vec.z);
    }

    public static Vector3 ReadVector3(this NetBuffer reader)
    {
        return new Vector3(
            reader.ReadFloat(),
            reader.ReadFloat(),
            reader.ReadFloat()
        );
    }
    
    public static void Write(this NetBuffer writer, Quaternion quat)
    {
        writer.Write(quat.x);
        writer.Write(quat.y);
        writer.Write(quat.z);
        writer.Write(quat.w);
    }

    public static Quaternion ReadQuaternion(this NetBuffer reader)
    {
        return new Quaternion(
            reader.ReadFloat(),
            reader.ReadFloat(),
            reader.ReadFloat(),
            reader.ReadFloat()
        );
    }

    public static void Write(this NetBuffer writer, IdentifierData id)
    {
        writer.Write((int)id.Type);
        writer.Write(id.Id);
    }

    public static IdentifierData ReadIdentifierData(this NetBuffer reader)
    {
        return new IdentifierData
        {
            Type = (IdentifierType)reader.ReadInt32(),
            Id = reader.ReadInt32(),
        };
    }
}