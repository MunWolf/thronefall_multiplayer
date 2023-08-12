using Steamworks;

namespace ThronefallMP.Network.Packets.Game;

public class CancelBuildPacket : BasePacket
{
    public const PacketId PacketID = PacketId.CancelBuild;
    
    public int BuildingId;

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
    }

    public override void Receive(Buffer reader)
    {
        BuildingId = reader.ReadInt32();
    }
}