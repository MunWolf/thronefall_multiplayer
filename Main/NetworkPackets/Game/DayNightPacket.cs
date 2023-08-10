using Steamworks;
using ThronefallMP.Network;

namespace ThronefallMP.NetworkPackets.Game;

public class DayNightPacket : IPacket
{
    public const PacketId PacketID = PacketId.DayNightPacket;

    public bool Night;

    public PacketId TypeID => PacketID;
    public int DeliveryMask => Constants.k_nSteamNetworkingSend_Reliable;
    public int Channel => 0;

    public void Send(Buffer writer)
    {
        writer.Write(Night);
    }

    public void Receive(Buffer reader)
    {
        Night = reader.ReadBoolean();
    }
}