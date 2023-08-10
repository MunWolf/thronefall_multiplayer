using Steamworks;
using ThronefallMP.Components;
using ThronefallMP.Network;

namespace ThronefallMP.NetworkPackets.Game;

public class HealPacket : IPacket
{
    public const PacketId PacketID = PacketId.HealPacket;

    public IdentifierData Target;
    public float Amount;

    public PacketId TypeID => PacketID;
    public int DeliveryMask => Constants.k_nSteamNetworkingSend_Reliable;
    public int Channel => 0;

    public void Send(Buffer writer)
    {
        writer.Write(Target);
        writer.Write(Amount);
    }

    public void Receive(Buffer reader)
    {
        Target = reader.ReadIdentifierData();
        Amount = reader.ReadFloat();
    }
}