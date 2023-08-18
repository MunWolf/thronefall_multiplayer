namespace ThronefallMP.Network.Packets.Game;

public class WeaponResponsePacket : BasePacket
{
    public const PacketId PacketID = PacketId.WeaponResponse;

    public Equipment Weapon;

    public override PacketId TypeID => PacketID;
    public override Channel Channel => Channel.Game;

    public override void Send(Buffer writer)
    {
        writer.Write(Weapon);
    }

    public override void Receive(Buffer reader)
    {
        Weapon = reader.ReadEquipment();
    }
}