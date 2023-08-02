using LiteNetLib;
using LiteNetLib.Utils;

namespace ThronefallMP.NetworkPackets;

public class DamagePacket : IPacket
{
    public const int PacketID = 7;

    public IdentifierData Target;
    public IdentifierData Source;
    public float Damage;
    public bool CausedByPlayer;
    public bool InvokeFeedbackEvents;

    public int TypeID()
    {
        return PacketID;
    }

    public void Send(ref NetDataWriter writer)
    {
        writer.Put((int)Target.Type);
        writer.Put(Target.Id);
        writer.Put((int)Source.Type);
        writer.Put(Source.Id);
        writer.Put(Damage);
        writer.Put(CausedByPlayer);
        writer.Put(InvokeFeedbackEvents);
    }

    public void Receive(ref NetPacketReader reader)
    {
        Target.Type = (IdentifierType)reader.GetInt();
        Target.Id = reader.GetInt();
        Source.Type = (IdentifierType)reader.GetInt();
        Source.Id = reader.GetInt();
        Damage = reader.GetFloat();
        CausedByPlayer = reader.GetBool();
        InvokeFeedbackEvents = reader.GetBool();
    }
}