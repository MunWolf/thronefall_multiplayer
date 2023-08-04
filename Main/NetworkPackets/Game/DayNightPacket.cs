using Lidgren.Network;
using ThronefallMP.Network;

namespace ThronefallMP.NetworkPackets.Game;

public class DayNightPacket : IPacket
{
    public const PacketId PacketID = PacketId.DayNightPacket;

    public bool Night;

    public PacketId TypeID => PacketID;
    public NetDeliveryMethod Delivery => NetDeliveryMethod.ReliableOrdered;
    public int Channel => 0;

    public void Send(NetBuffer writer)
    {
        writer.Write(Night);
    }

    public void Receive(NetBuffer reader)
    {
        Night = reader.ReadBoolean();
    }
}