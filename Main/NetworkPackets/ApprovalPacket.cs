using Steamworks;
using ThronefallMP.Components;
using ThronefallMP.Network;

namespace ThronefallMP.NetworkPackets;

public class ApprovalPacket : IPacket
{
    public const PacketId PacketID = PacketId.ApprovalPacket;

    public bool SameVersion;
    public string Password;

    public PacketId TypeID => PacketID;
    public int DeliveryMask => Constants.k_nSteamNetworkingSend_Reliable;
    public int Channel => 0;
    
    public void Send(Buffer writer)
    {
        writer.Write(Plugin.VersionString);
        writer.Write(Password);
    }

    public void Receive(Buffer reader)
    {
        SameVersion = reader.ReadString() == Plugin.VersionString;
        Password = reader.ReadString();
    }
}