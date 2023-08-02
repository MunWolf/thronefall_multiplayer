using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;

namespace ThronefallMP.NetworkPackets;

public class CommandAddPacket : IPacket
{
    public const PacketId PacketID = PacketId.CommandAddPacket;

    public int Player;
    public List<IdentifierData> Units = new();

    public PacketId TypeID()
    {
        return PacketID;
    }

    public void Send(ref NetDataWriter writer)
    {
        writer.Put(Player);
        writer.Put(Units.Count);
        foreach (var unit in Units)
        {
            writer.Put(unit);
        }
    }

    public void Receive(ref NetPacketReader reader)
    {
        Player = reader.GetInt();
        var count = reader.GetInt();
        Units.Clear();
        for (var i = 0; i < count; ++i)
        {
            Units.Add(reader.GetIdentifierData());
        }
    }
}