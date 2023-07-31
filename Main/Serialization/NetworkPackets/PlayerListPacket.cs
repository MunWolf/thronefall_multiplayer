using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;

namespace ThronefallMP.NetworkPackets;

public class PlayerListPacket : IPacket
{
    public const int PacketID = 1;

    public List<int> PlayerIDs = new();

    public int TypeID()
    {
        return PacketID;
    }

    public void Send(ref NetDataWriter writer)
    {
        writer.Put(PlayerIDs.Count);
        foreach (var id in PlayerIDs)
        {
            writer.Put(id);
        }
    }

    public void Receive(ref NetPacketReader reader)
    {
        PlayerIDs.Clear();
        var count = reader.GetInt();
        for (var i = 0; i < count; ++i)
        {
            PlayerIDs.Add(reader.GetInt());
        }
    }
}