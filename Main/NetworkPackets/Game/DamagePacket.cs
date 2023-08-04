using Lidgren.Network;
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
    public NetDeliveryMethod Delivery => NetDeliveryMethod.ReliableOrdered;
    public int Channel => 0;

    public void Send(NetBuffer writer)
    {
        writer.Write(Target);
        writer.Write(Source);
        writer.Write(Damage);
        writer.Write(CausedByPlayer);
        writer.Write(InvokeFeedbackEvents);
    }

    public void Receive(NetBuffer reader)
    {
        Target = reader.ReadIdentifierData();
        Source = reader.ReadIdentifierData();
        Damage = reader.ReadFloat();
        CausedByPlayer = reader.ReadBoolean();
        InvokeFeedbackEvents = reader.ReadBoolean();
    }
}