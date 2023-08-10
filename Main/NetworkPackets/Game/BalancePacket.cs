using Steamworks;
using ThronefallMP.Network;

namespace ThronefallMP.NetworkPackets.Game;

public class BalancePacket : IPacket
{
    public const PacketId PacketID = PacketId.BalancePacket;

    public int Delta;
    
    public PacketId TypeID => PacketID;
    public int DeliveryMask => Constants.k_nSteamNetworkingSend_Reliable;
    public int Channel => 0;

    public void Send(Buffer writer)
    {
        writer.Write(Delta);
    }

    public void Receive(Buffer reader)
    {
        Delta = reader.ReadInt32();
    }
}