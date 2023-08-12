using Steamworks;

namespace ThronefallMP.Network.Packets.Game;

public class ManualAttackPacket : BasePacket
{
    public const PacketId PacketID = PacketId.ManualAttack;

    public int Player;

    public override PacketId TypeID => PacketID;
    public override Channel Channel => Channel.Player;
    public override bool ShouldPropagate => true;

    public override void Send(Buffer writer)
    {
        writer.Write(Player);
    }

    public override void Receive(Buffer reader)
    {
        Player = reader.ReadInt32();
    }
}