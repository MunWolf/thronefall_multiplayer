using Steamworks;
using ThronefallMP.Network;

namespace ThronefallMP.NetworkPackets.Game;

public class BuildOrUpgradePacket : IPacket
{
    public const PacketId PacketID = PacketId.BuildOrUpgradePacket;

    public int BuildingId;
    public int Level;
    public int Choice;

    public PacketId TypeID => PacketID;
    public int DeliveryMask => Constants.k_nSteamNetworkingSend_Reliable;
    public int Channel => 0;

    public void Send(Buffer writer)
    {
        writer.Write(BuildingId);
        writer.Write(Level);
        writer.Write(Choice);
    }

    public void Receive(Buffer reader)
    {
        BuildingId = reader.ReadInt32();
        Level = reader.ReadInt32();
        Choice = reader.ReadInt32();
    }
}