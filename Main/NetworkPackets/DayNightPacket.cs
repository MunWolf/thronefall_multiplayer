using LiteNetLib;
using LiteNetLib.Utils;

namespace ThronefallMP.NetworkPackets;

public class DayNightPacket : IPacket
{
    public const int PacketID = 5;

    public bool Night;

    public int TypeID()
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