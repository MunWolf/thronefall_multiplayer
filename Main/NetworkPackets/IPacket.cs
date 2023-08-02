using LiteNetLib;
using LiteNetLib.Utils;

namespace ThronefallMP.NetworkPackets;

public interface IPacket
{
    PacketId TypeID();
    void Send(ref NetDataWriter writer);
    void Receive(ref NetPacketReader reader);
}