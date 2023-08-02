using LiteNetLib;
using LiteNetLib.Utils;

namespace ThronefallMP.NetworkPackets;

public class ManualAttackPacket : IPacket
{
    public const PacketId PacketID = PacketId.ManualAttack;

    public int Player;

    public PacketId TypeID()
    {
        return PacketID;
    }

    public void Send(ref NetDataWriter writer)
    {
        writer.Put(Player);
    }

    public void Receive(ref NetPacketReader reader)
    {
        Player = reader.GetInt();
    }
}