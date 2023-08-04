using Lidgren.Network;
using ThronefallMP.Components;
using ThronefallMP.Network;
using UnityEngine;

namespace ThronefallMP.NetworkPackets.Game;

public class PositionPacket : IPacket
{
    public const PacketId PacketID = PacketId.PositionPacket;

    public IdentifierData Target;
    public Vector3 Position;

    public PacketId TypeID => PacketID;
    public NetDeliveryMethod Delivery => NetDeliveryMethod.ReliableOrdered;
    public int Channel => 0;

    public void Send(NetBuffer writer)
    {
        writer.Write((int)Target.Type);
        writer.Write(Target.Id);
        writer.Write(Position);
    }

    public void Receive(NetBuffer reader)
    {
        Target.Type = (IdentifierType)reader.ReadInt32();
        Target.Id = reader.ReadInt32();
        Position = reader.ReadVector3();
    }
}