using Steamworks;
using ThronefallMP.Network;

namespace ThronefallMP.NetworkPackets.Game;

public class ManualAttackPacket : IPacket
{
    public const PacketId PacketID = PacketId.ManualAttack;

    public int Player;

    public PacketId TypeID => PacketID;
    public int DeliveryMask => Constants.k_nSteamNetworkingSend_Reliable;
    public int Channel => 0;

    public void Send(Buffer writer)
    {
        writer.Write(Player);
    }

    public void Receive(Buffer reader)
    {
        Player = reader.ReadInt32();
    }
}