using System.Collections.Generic;

namespace ThronefallMP.Network.Packets.Sync;

public class SyncLevelDataPacket : BasePacket
{
    public struct Player
    {
        public ushort PlayerId;
        public byte SpawnId;
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
            writer.Write(perk);
        }
        
        writer.Write((byte)PlayerData.Count);
        foreach (var player in PlayerData)
        {
            writer.Write(player.PlayerId);
            writer.Write(player.SpawnId);
            writer.Write(player.Weapon);
        }
    }

    public override void Receive(Buffer reader)
    {
        PlayerData.Clear();
        Level = reader.ReadString();
        
        var perks = reader.ReadByte();
        Perks.Clear();
        for (var i = 0; i < perks; ++i)
        {
            Perks.Add(reader.ReadEquipment());
        }
        
        var spawns = reader.ReadInt32();
        for (var i = 0; i < spawns; ++i)
        {
            PlayerData.Add(new Player{
                PlayerId = reader.ReadUInt16(),
                SpawnId = reader.ReadByte(),
                Weapon = reader.ReadEquipment()
            });
        }
    }
}