using Steamworks;
using ThronefallMP.Network;

namespace ThronefallMP.NetworkPackets;

public class DisconnectPacket : IPacket
{
    public enum Reason
    {
        Kicked,
        WrongPassword,
        WrongVersion
    }
    
    public const PacketId PacketID = PacketId.DisconnectPacket;

    public Reason DisconnectReason;

    public PacketId TypeID => PacketID;
    public int DeliveryMask => Constants.k_nSteamNetworkingSend_Reliable | Constants.k_nSteamNetworkingSend_NoNagle;
    public int Channel => 0;
    
    public void Send(Buffer writer)
    {
        writer.Write((int)DisconnectReason);
    }

    public void Receive(Buffer reader)
    {
        DisconnectReason = (Reason)reader.ReadInt32();
    }
}