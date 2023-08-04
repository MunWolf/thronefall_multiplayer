using Lidgren.Network;
using ThronefallMP.Components;
using ThronefallMP.Network;

namespace ThronefallMP.NetworkPackets.Game;

public class ScaleHpPacket : IPacket
{
    public const PacketId PacketID = PacketId.ScaleHpPacket;

    public IdentifierData Target;
    public float Multiplier;

    public PacketId TypeID => PacketID;
    public NetDeliveryMethod Delivery => NetDeliveryMethod.ReliableOrdered;
    public int Channel => 0;

    public void Send(NetBuffer writer)
    {
        writer.Write((int)Target.Type);
        writer.Write(Target.Id);
        writer.Write(Multiplier);
    }

    public void Receive(NetBuffer reader)
    {
        Target.Type = (IdentifierType)reader.ReadInt32();
        Target.Id = reader.ReadInt32();
        Multiplier = reader.ReadFloat();
    }
}