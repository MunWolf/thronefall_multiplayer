using LiteNetLib;
using LiteNetLib.Utils;
using ThronefallMP.Network;

namespace ThronefallMP.NetworkPackets;

public interface IPacket
{
    PacketId TypeID();
    void Send(ref NetDataWriter writer);
    void Receive(ref NetPacketReader reader);
}