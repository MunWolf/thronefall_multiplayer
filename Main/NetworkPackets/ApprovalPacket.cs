using Steamworks;
using ThronefallMP.Components;
using ThronefallMP.Network;

namespace ThronefallMP.NetworkPackets;

public class ApprovalPacket : IPacket
{
    private const string ApprovalString = $"thronefall_mp_{PluginInfo.PLUGIN_VERSION}";
    
    public const PacketId PacketID = PacketId.ApprovalPacket;

    public string Password;

    public PacketId TypeID => PacketID;
    public int DeliveryMask => Constants.k_nSteamNetworkingSend_Reliable;
    public int Channel => 0;
    
    public void Send(Buffer writer)
    {
        writer.Write(Password);
    }

    public void Receive(Buffer reader)
    {
        Password = reader.ReadString();
    }
}