using Steamworks;

namespace ThronefallMP.Network.Packets;

public abstract class BasePacket
{
    public abstract PacketId TypeID { get; }
    public virtual int DeliveryMask => Constants.k_nSteamNetworkingSend_ReliableNoNagle;
    public abstract Channel Channel { get; }
    public virtual bool ShouldPropagate => false;

    public virtual bool CanHandle(CSteamID sender)
    {
        return true;
    }

    public abstract void Send(Buffer writer);
    public abstract void Receive(Buffer reader);
}