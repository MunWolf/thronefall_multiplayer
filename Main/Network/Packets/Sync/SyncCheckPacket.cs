using Steamworks;

namespace ThronefallMP.Network.Packets.Sync;

public class SyncCheckPacket : BasePacket
{
    public const PacketId PacketID = PacketId.SyncCheck;
    
    public override PacketId TypeID => PacketID;
    public override Channel Channel => Channel.Game;
    public override int DeliveryMask => Constants.k_nSteamNetworkingSend_Unreliable;

    public int Type;
    public int Order;
    public int Hash;
    
    public override void Send(Buffer writer)
    {
        writer.Write(Type);
        writer.Write(Order);
        writer.Write(Hash);
    }

    public override void Receive(Buffer reader)
    {
        Type = reader.ReadInt32();
        Order = reader.ReadInt32();
        Hash = reader.ReadInt32();
    }
}