using LiteNetLib;
using LiteNetLib.Utils;

namespace ThronefallMP.NetworkPackets;

public class BuildOrUpgradePacket : IPacket
{
    public const PacketId PacketID = PacketId.BuildOrUpgradePacket;

    public int BuildingId;
    public int Level;
    public int Choice;

    public PacketId TypeID()
    {
        return PacketID;
    }

    public void Send(ref NetDataWriter writer)
    {
        writer.Put(BuildingId);
        writer.Put(Level);
        writer.Put(Choice);
    }

    public void Receive(ref NetPacketReader reader)
    {
        BuildingId = reader.GetInt();
        Level = reader.GetInt();
        Choice = reader.GetInt();
    }
}