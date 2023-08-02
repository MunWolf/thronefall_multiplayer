using LiteNetLib;
using LiteNetLib.Utils;

namespace ThronefallMP.NetworkPackets;

public class ScaleHpPacket : IPacket
{
    public const int PacketID = 9;

    public IdentifierData Target;
    public float Multiplier;

    public int TypeID()
    {
        return PacketID;
    }

    public void Send(ref NetDataWriter writer)
    {
        writer.Put((int)Target.Type);
        writer.Put(Target.Id);
        writer.Put(Multiplier);
    }

    public void Receive(ref NetPacketReader reader)
    {
        Target.Type = (IdentifierType)reader.GetInt();
        Target.Id = reader.GetInt();
        Multiplier = reader.GetFloat();
    }
}