using Steamworks;

namespace ThronefallMP.Network.Packets.PlayerCommand;

public class ConfirmBuildPacket : BasePacket
{
    public const PacketId PacketID = PacketId.ConfirmBuild;
    
    public ushort PlayerID;
    public ushort BuildingId;
    public byte Level;
    public byte Choice;

    public override PacketId TypeID => PacketID;
    public override int DeliveryMask => Constants.k_nSteamNetworkingSend_Reliable;
    public override Channel Channel => Channel.Resources;
    public override bool CanHandle(CSteamID sender)
    {
        return Plugin.Instance.Network.IsServer(sender);
    }

    public override void Send(Buffer writer)
    {
        writer.Write(PlayerID);
        writer.Write(BuildingId);
        writer.Write(Level);
        writer.Write(Choice);
    }

    public override void Receive(Buffer reader)
    {
        PlayerID = reader.ReadUInt16();
        BuildingId = reader.ReadUInt16();
        Level = reader.ReadByte();
        Choice = reader.ReadByte();
    }
}