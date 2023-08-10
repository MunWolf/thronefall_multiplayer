using Steamworks;
using ThronefallMP.Components;
using ThronefallMP.Network;

namespace ThronefallMP.NetworkPackets.Game;

public class ScaleHpPacket : IPacket
{
    public const PacketId PacketID = PacketId.ScaleHpPacket;

    public IdentifierData Target;
    public float Multiplier;

    public PacketId TypeID => PacketID;
    public int DeliveryMask => Constants.k_nSteamNetworkingSend_Reliable;
    public int Channel => 0;

    public void Send(Buffer writer)
    {
        writer.Write((int)Target.Type);
        writer.Write(Target.Id);
        writer.Write(Multiplier);
    }

    public void Receive(Buffer reader)
    {
        Target.Type = (IdentifierType)reader.ReadInt32();
        Target.Id = reader.ReadInt32();
        Multiplier = reader.ReadFloat();
    }
}