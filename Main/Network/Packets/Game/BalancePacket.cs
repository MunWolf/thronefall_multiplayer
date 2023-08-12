using Rewired;
using Steamworks;

namespace ThronefallMP.Network.Packets.Game;

public class BalancePacket : BasePacket
{
    public const PacketId PacketID = PacketId.Balance;

    public int Balance;
    public int Networth;
    
    public override PacketId TypeID => PacketID;
    public override int DeliveryMask => Constants.k_nSteamNetworkingSend_Reliable;
    public override Channel Channel => Channel.Resources;
    public override bool CanHandle(CSteamID sender)
    {
        return Plugin.Instance.Network.IsServer(sender);
    }

    public override void Send(Buffer writer)
    {
        writer.Write(Balance);
        writer.Write(Networth);
    }

    public override void Receive(Buffer reader)
    {
        Balance = reader.ReadInt32();
        Networth = reader.ReadInt32();
    }
}