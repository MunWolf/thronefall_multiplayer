using LiteNetLib;
using LiteNetLib.Utils;
using ThronefallMP.Components;
using ThronefallMP.Network;
using UnityEngine;

namespace ThronefallMP.NetworkPackets;

public class PositionPacket : IPacket
{
    public const PacketId PacketID = PacketId.PositionPacket;

    public IdentifierData Target;
    public Vector3 Position;

    public PacketId TypeID()
    {
        return PacketID;
    }

    public void Send(ref NetDataWriter writer)
    {
        writer.Put((int)Target.Type);
        writer.Put(Target.Id);
        writer.Put(Position);
    }

    public void Receive(ref NetPacketReader reader)
    {
        Target.Type = (IdentifierType)reader.GetInt();
        Target.Id = reader.GetInt();
        Position = reader.GetVector3();
    }
}