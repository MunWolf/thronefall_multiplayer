using Lidgren.Network;
using ThronefallMP.Network;

namespace ThronefallMP.NetworkPackets.Game;

public class BuildOrUpgradePacket : IPacket
{
    public const PacketId PacketID = PacketId.BuildOrUpgradePacket;

    public int BuildingId;
    public int Level;
    public int Choice;

    public PacketId TypeID => PacketID;
    public NetDeliveryMethod Delivery => NetDeliveryMethod.ReliableOrdered;
    public int Channel => 0;

    public void Send(NetBuffer writer)
    {
        writer.Write(BuildingId);
        writer.Write(Level);
        writer.Write(Choice);
    }

    public void Receive(NetBuffer reader)
    {
        BuildingId = reader.ReadInt32();
        Level = reader.ReadInt32();
        Choice = reader.ReadInt32();
    }
}