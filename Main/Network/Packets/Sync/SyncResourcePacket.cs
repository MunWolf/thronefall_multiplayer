using Steamworks;

namespace ThronefallMP.Network.Packets.Sync;

public class SyncResourcePacket : BasePacket
{
    public const PacketId PacketID = PacketId.SyncResource;
    public override PacketId TypeID => PacketID;
    public override Channel Channel => Channel.SyncPlayer;
    public override int DeliveryMask => Constants.k_nSteamNetworkingSend_Reliable;

    public int Balance;
    public int NetWorth;
    
    public override void Send(Buffer writer)
    {
        writer.Write(Balance);
        writer.Write(NetWorth);
    }

    public override void Receive(Buffer reader)
    {
        Balance = reader.ReadInt32();
        NetWorth = reader.ReadInt32();
    }
}