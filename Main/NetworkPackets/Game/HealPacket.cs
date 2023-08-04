using Lidgren.Network;
using ThronefallMP.Components;
using ThronefallMP.Network;

namespace ThronefallMP.NetworkPackets.Game;

public class HealPacket : IPacket
{
    public const PacketId PacketID = PacketId.HealPacket;

    public IdentifierData Target;
    public float Amount;

    public PacketId TypeID => PacketID;
    public NetDeliveryMethod Delivery => NetDeliveryMethod.ReliableOrdered;
    public int Channel => 0;

    public void Send(NetBuffer writer)
    {
        writer.Write(Target);
        writer.Write(Amount);
    }

    public void Receive(NetBuffer reader)
    {
        Target = reader.ReadIdentifierData();
        Amount = reader.ReadFloat();
    }
}