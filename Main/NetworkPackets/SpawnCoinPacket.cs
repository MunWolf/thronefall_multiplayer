using LiteNetLib;
using LiteNetLib.Utils;

namespace ThronefallMP.NetworkPackets;

public class SpawnCoinPacket : IPacket
{
    public const PacketId PacketID = PacketId.SpawnCoinPacket;

    public PacketId TypeID()
    {
        return PacketID;
    }

    public void Send(ref NetDataWriter writer)
    {
        
    }

    public void Receive(ref NetPacketReader reader)
    {
        
    }
}