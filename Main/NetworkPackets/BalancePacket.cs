using LiteNetLib;
using LiteNetLib.Utils;
using ThronefallMP.Network;

namespace ThronefallMP.NetworkPackets;

public class BalancePacket : IPacket
{
    public const PacketId PacketID = PacketId.BalancePacket;

    public int Delta;
    
    public PacketId TypeID()
    {
        return PacketID;
    }

    public void Send(ref NetDataWriter writer)
    {
        writer.Put(Delta);
    }

    public void Receive(ref NetPacketReader reader)
    {
        Delta = reader.GetInt();
    }
}