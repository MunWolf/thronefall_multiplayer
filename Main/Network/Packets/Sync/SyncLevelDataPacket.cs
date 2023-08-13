using System.Collections.Generic;

namespace ThronefallMP.Network.Packets.Sync;

public class SyncLevelDataPacket : BasePacket
{
    public const PacketId PacketID = PacketId.SyncLevelData;

    public string Level;
    public List<Equipment> Perks = new();
    public List<(int playerId, int spawnId)> Spawns = new();
    
    public override PacketId TypeID => PacketID;
    public override Channel Channel => Channel.Player;

    public override void Send(Buffer writer)
    {
        writer.Write(Level);
        writer.Write(Perks.Count);
        foreach (var perk in Perks)
        {
            writer.Write((int)perk);
        }
        
        writer.Write(Spawns.Count);
        foreach (var spawn in Spawns)
        {
            writer.Write(spawn.playerId);
            writer.Write(spawn.spawnId);
        }
    }

    public override void Receive(Buffer reader)
    {
        Spawns.Clear();
        Level = reader.ReadString();
        
        var perks = reader.ReadInt32();
        Perks.Clear();
        for (var i = 0; i < perks; ++i)
        {
            Perks.Add((Equipment)reader.ReadInt32());
        }
        
        var spawns = reader.ReadInt32();
        for (var i = 0; i < spawns; ++i)
        {
            Spawns.Add((
                reader.ReadInt32(),
                reader.ReadInt32()
            ));
        }
    }
}