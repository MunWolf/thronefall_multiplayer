using LiteNetLib;
using LiteNetLib.Utils;

namespace ThronefallMP.NetworkPackets;

public class HealPacket : IPacket
{
    public const PacketId PacketID = PacketId.HealPacket;

    public IdentifierData Target;
    public float Amount;

    public PacketId TypeID()
    {
        return PacketID;
    }

    public void Send(ref NetDataWriter writer)
    {
        writer.Put(Target);
        writer.Put(Amount);
    }

    public void Receive(ref NetPacketReader reader)
    {
        Target = reader.GetIdentifierData();
        Amount = reader.GetFloat();
    }
}