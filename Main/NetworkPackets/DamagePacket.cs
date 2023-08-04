using LiteNetLib;
using LiteNetLib.Utils;
using ThronefallMP.Components;
using ThronefallMP.Network;

namespace ThronefallMP.NetworkPackets;

public class DamagePacket : IPacket
{
    public const PacketId PacketID = PacketId.DamagePacket;

    public IdentifierData Target;
    public IdentifierData Source;
    public float Damage;
    public bool CausedByPlayer;
    public bool InvokeFeedbackEvents;

    public PacketId TypeID()
    {
        return PacketID;
    }

    public void Send(ref NetDataWriter writer)
    {
        writer.Put(Target);
        writer.Put(Source);
        writer.Put(Damage);
        writer.Put(CausedByPlayer);
        writer.Put(InvokeFeedbackEvents);
    }

    public void Receive(ref NetPacketReader reader)
    {
        Target = reader.GetIdentifierData();
        Source = reader.GetIdentifierData();
        Damage = reader.GetFloat();
        CausedByPlayer = reader.GetBool();
        InvokeFeedbackEvents = reader.GetBool();
    }
}