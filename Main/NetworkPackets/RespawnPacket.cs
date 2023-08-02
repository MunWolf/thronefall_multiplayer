using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace ThronefallMP.NetworkPackets;

public class RespawnPacket : IPacket
{
    public const int PacketID = 10;

    public IdentifierData Target;
    public Vector3 Position;

    public int TypeID()
    {
        return PacketID;
    }

    public void Send(ref NetDataWriter writer)
    {
        writer.Put((int)Target.Type);
        writer.Put(Target.Id);
        writer.Put(Position.x);
        writer.Put(Position.y);
        writer.Put(Position.z);
    }

    public void Receive(ref NetPacketReader reader)
    {
        Target.Type = (IdentifierType)reader.GetInt();
        Target.Id = reader.GetInt();
        Position.x = reader.GetFloat();
        Position.y = reader.GetFloat();
        Position.z = reader.GetFloat();
    }
}