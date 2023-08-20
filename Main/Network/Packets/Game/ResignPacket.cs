namespace ThronefallMP.Network.Packets.Game;

public class ResignPacket : BasePacket
{
    public const PacketId PacketID = PacketId.Resign;

    public override PacketId TypeID => PacketID;
    public override Channel Channel => Channel.Game;
    public override bool ShouldPropagate => true;

    public override void Send(Buffer writer) { }

    public override void Receive(Buffer reader) { }
}