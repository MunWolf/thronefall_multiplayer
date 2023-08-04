using LiteNetLib;
using LiteNetLib.Utils;
using ThronefallMP.Network;

namespace ThronefallMP.NetworkPackets;

public class DayNightPacket : IPacket
{
    public const PacketId PacketID = PacketId.DayNightPacket;

    public bool Night;

    public PacketId TypeID()
    {
        return PacketID;
    }

    public void Send(ref NetDataWriter writer)
    {
        writer.Put(Night);
    }

    public void Receive(ref NetPacketReader reader)
    {
        Night = reader.GetBool();
    }
}