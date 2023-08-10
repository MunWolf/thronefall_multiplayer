using Steamworks;
using ThronefallMP.Components;
using ThronefallMP.Network;

namespace ThronefallMP.NetworkPackets.Game;

public class DamagePacket : IPacket
{
    public const PacketId PacketID = PacketId.DamagePacket;

    public IdentifierData Target;
    public IdentifierData Source;
    public float Damage;
    public bool CausedByPlayer;
    public bool InvokeFeedbackEvents;

    public PacketId TypeID => PacketID;
    public int DeliveryMask => Constants.k_nSteamNetworkingSend_Reliable;
    public int Channel => 0;

    public void Send(Buffer writer)
    {
        writer.Write(Target);
        writer.Write(Source);
        writer.Write(Damage);
        writer.Write(CausedByPlayer);
        writer.Write(InvokeFeedbackEvents);
    }

    public void Receive(Buffer reader)
    {
        Target = reader.ReadIdentifierData();
        Source = reader.ReadIdentifierData();
        Damage = reader.ReadFloat();
        CausedByPlayer = reader.ReadBoolean();
        InvokeFeedbackEvents = reader.ReadBoolean();
    }
}