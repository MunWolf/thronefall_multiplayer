namespace ThronefallMP.Network.Packets.Game;

public class WeaponRequestPacket : BasePacket
{
    public const PacketId PacketID = PacketId.WeaponRequest;

    public override PacketId TypeID => PacketID;
    public override Channel Channel => Channel.Game;

    public override void Send(Buffer writer) {}

    public override void Receive(Buffer reader) {}
}