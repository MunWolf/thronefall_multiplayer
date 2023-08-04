using Lidgren.Network;
using ThronefallMP.Network;

namespace ThronefallMP.NetworkPackets.Game;

public class ManualAttackPacket : IPacket
{
    public const PacketId PacketID = PacketId.ManualAttack;

    public int Player;

    public PacketId TypeID => PacketID;
    public NetDeliveryMethod Delivery => NetDeliveryMethod.ReliableOrdered;
    public int Channel => 0;

    public void Send(NetBuffer writer)
    {
        writer.Write(Player);
    }

    public void Receive(NetBuffer reader)
    {
        Player = reader.ReadInt32();
    }
}