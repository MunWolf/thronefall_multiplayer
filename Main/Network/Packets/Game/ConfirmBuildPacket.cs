using Steamworks;

namespace ThronefallMP.Network.Packets.Game;

public class ConfirmBuildPacket : BasePacket
{
    public const PacketId PacketID = PacketId.ConfirmBuild;
    
    public int BuildingId;
    public int Level;
    public int Choice;
    public int PlayerID;

    public override PacketId TypeID => PacketID;
    public override int DeliveryMask => Constants.k_nSteamNetworkingSend_Reliable;
    public override Channel Channel => Channel.Resources;
    public override bool CanHandle(CSteamID sender)
    {
        return Plugin.Instance.Network.IsServer(sender);
    }

    public override void Send(Buffer writer)
    {
        writer.Write(BuildingId);
        writer.Write(Level);
        writer.Write(Choice);
        writer.Write(PlayerID);
    }

    public override void Receive(Buffer reader)
    {
        BuildingId = reader.ReadInt32();
        Level = reader.ReadInt32();
        Choice = reader.ReadInt32();
        PlayerID = reader.ReadInt32();
    }
}