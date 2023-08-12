using Steamworks;

namespace ThronefallMP.Network.Packets;

// Allows for ordered packets in unreliable contexts
public abstract class BaseOrderedPacket : BasePacket
{
    private static int _next;

    private int _sentByPlayer = Plugin.Instance.PlayerManager.LocalId;
    private int _count = _next++;

    public override int DeliveryMask => Constants.k_nSteamNetworkingSend_UnreliableNoNagle;

    public (int player, int count) GetOrder()
    {
        return (_sentByPlayer, _count);
    }
    
    public override void Send(Buffer writer)
    {
        writer.Write(_count);
        writer.Write(_sentByPlayer);
    }

    public override void Receive(Buffer reader)
    {
        _count = reader.ReadInt32();
        _sentByPlayer = reader.ReadInt32();
    }
}