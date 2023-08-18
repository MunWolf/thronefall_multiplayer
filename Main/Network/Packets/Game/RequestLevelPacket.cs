using System.Collections.Generic;

namespace ThronefallMP.Network.Packets.Game;

public class RequestLevelPacket : BasePacket
{
    public const PacketId PacketID = PacketId.RequestLevel;

    public string To;
    public string From;
    public Equipment SelectedWeapon;
    public List<Equipment> Perks = new();
    
    public override PacketId TypeID => PacketID;
    public override Channel Channel => Channel.Game;

    public override void Send(Buffer writer)
    {
        writer.Write(To);
        writer.Write(From);
        writer.Write((byte)Perks.Count);
        foreach (var perk in Perks)
        {
            writer.Write(perk);
        }
    }

    public override void Receive(Buffer reader)
    {
        To = reader.ReadString();
        From = reader.ReadString();
        var perks = reader.ReadByte();
        Perks.Clear();
        for (var i = 0; i < perks; ++i)
        {
            Perks.Add(reader.ReadEquipment());
        }
    }
}