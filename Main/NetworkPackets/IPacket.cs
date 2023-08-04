using Lidgren.Network;
using ThronefallMP.Network;

namespace ThronefallMP.NetworkPackets;

public interface IPacket
{
    PacketId TypeID { get; }
    NetDeliveryMethod Delivery { get; }
    int Channel { get; }
    
    void Send(NetBuffer writer);
    void Receive(NetBuffer reader);
}