using Steamworks;

namespace ThronefallMP.Network.Packets.PlayerCommand;

public class BuildOrUpgradePacket : BasePacket
{
    public const PacketId PacketID = PacketId.BuildOrUpgrade;

    public int BuildingId;
    public int Level;
    public int Choice;

    public override PacketId TypeID => PacketID;
    public override int DeliveryMask => Constants.k_nSteamNetworkingSend_Reliable;
    public override Channel Channel => Channel.Resources;
    public override bool CanHandle(CSteamID sender)
    {
        return Plugin.Instance.Network.Server;
    }

    public override void Send(Buffer writer)
    {
        writer.Write(BuildingId);
        writer.Write(Level);
        writer.Write(Choice);
    }

    public override void Receive(Buffer reader)
    {
        BuildingId = reader.ReadInt32();
        Level = reader.ReadInt32();
        Choice = reader.ReadInt32();
    }
}