using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;

namespace ThronefallMP.NetworkPackets;

public class TransitionToScenePacket : IPacket
{
    public enum TransitionType
    {
        LevelSelectToLevel
    }
    
    public const PacketId PacketID = PacketId.TransitionToScenePacket;

    public TransitionType Type;
    public string Level;
    public List<string> Perks = new();

    public PacketId TypeID()
    {
        return PacketID;
    }

    public void Send(ref NetDataWriter writer)
    {
        writer.Put((int)Type);
        writer.Put(Level);
        writer.Put(Perks.Count);
        foreach (var perk in Perks)
        {
            writer.Put(perk);
        }
    }

    public void Receive(ref NetPacketReader reader)
    {
        Type = (TransitionType)reader.GetInt();
        Level = reader.GetString();
        var count = reader.GetInt();
        Perks.Clear();
        for (var i = 0; i < count; ++i)
        {
            Perks.Add(reader.GetString());
        }
    }
}