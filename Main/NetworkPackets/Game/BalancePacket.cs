using Lidgren.Network;
using ThronefallMP.Network;

namespace ThronefallMP.NetworkPackets.Game;

public class BalancePacket : IPacket
{
    public const PacketId PacketID = PacketId.BalancePacket;

    public int Delta;
    
    public PacketId TypeID => PacketID;
    public NetDeliveryMethod Delivery => NetDeliveryMethod.ReliableOrdered;
    public int Channel => 0;

    public void Send(NetBuffer writer)
    {
        writer.Write(Delta);
    }

    public void Receive(NetBuffer reader)
    {
        Delta = reader.ReadInt32();
    }
}