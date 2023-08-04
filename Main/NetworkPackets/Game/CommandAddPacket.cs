using System.Collections.Generic;
using Lidgren.Network;
using ThronefallMP.Components;
using ThronefallMP.Network;

namespace ThronefallMP.NetworkPackets.Game;

public class CommandAddPacket : IPacket
{
    public const PacketId PacketID = PacketId.CommandAddPacket;

    public int Player;
    public List<IdentifierData> Units = new();

    public PacketId TypeID => PacketID;
    public NetDeliveryMethod Delivery => NetDeliveryMethod.ReliableOrdered;
    public int Channel => 0;

    public void Send(NetBuffer writer)
    {
        writer.Write(Player);
        writer.Write(Units.Count);
        foreach (var unit in Units)
        {
            writer.Write(unit);
        }
    }

    public void Receive(NetBuffer reader)
    {
        Player = reader.ReadInt32();
        var count = reader.ReadInt32();
        Units.Clear();
        for (var i = 0; i < count; ++i)
        {
            Units.Add(reader.ReadIdentifierData());
        }
    }
}