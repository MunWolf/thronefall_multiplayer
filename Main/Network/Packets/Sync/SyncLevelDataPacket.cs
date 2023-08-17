using System.Collections.Generic;

namespace ThronefallMP.Network.Packets.Sync;

public class SyncLevelDataPacket : BasePacket
{
    public struct Player
    {
        public int PlayerId;
        public int SpawnId;
        public Equipment Weapon;
    }
    
    public const PacketId PacketID = PacketId.SyncLevelData;

    public string Level;
    public List<Equipment> Perks = new();
    public List<Player> PlayerData = new();
    
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
        
        writer.Write(PlayerData.Count);
        foreach (var player in PlayerData)
        {
            writer.Write(player.PlayerId);
            writer.Write(player.SpawnId);
            writer.Write((int)player.Weapon);
        }
    }

    public override void Receive(Buffer reader)
    {
        PlayerData.Clear();
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
            PlayerData.Add(new Player{
                PlayerId = reader.ReadInt32(),
                SpawnId = reader.ReadInt32(),
                Weapon = (Equipment)reader.ReadInt32()
            });
        }
    }
}