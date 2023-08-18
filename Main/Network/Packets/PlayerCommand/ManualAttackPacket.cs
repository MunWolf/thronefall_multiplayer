namespace ThronefallMP.Network.Packets.PlayerCommand;

public class ManualAttackPacket : BasePacket
{
    public const PacketId PacketID = PacketId.ManualAttack;

    public ushort Player;

    public override PacketId TypeID => PacketID;
    public override Channel Channel => Channel.Player;
    public override bool ShouldPropagate => true;

    public override void Send(Buffer writer)
    {
        writer.Write(Player);
    }

    public override void Receive(Buffer reader)
    {
        Player = reader.ReadUInt16();
    }
}