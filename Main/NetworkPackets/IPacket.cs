using ThronefallMP.Network;

namespace ThronefallMP.NetworkPackets;

public interface IPacket
{
    PacketId TypeID { get; }
    int DeliveryMask { get; }
    int Channel { get; }
    
    void Send(Buffer writer);
    void Receive(Buffer reader);
}