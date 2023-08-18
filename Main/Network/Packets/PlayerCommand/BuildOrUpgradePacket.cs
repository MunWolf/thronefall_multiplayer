using Steamworks;

namespace ThronefallMP.Network.Packets.PlayerCommand;

public class BuildOrUpgradePacket : BasePacket
{
    public const PacketId PacketID = PacketId.BuildOrUpgrade;

    public ushort BuildingId;
    public byte Level;
    public byte Choice;

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
        BuildingId = reader.ReadUInt16();
        Level = reader.ReadByte();
        Choice = reader.ReadByte();
    }
}