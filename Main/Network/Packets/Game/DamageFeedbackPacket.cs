using ThronefallMP.Components;

namespace ThronefallMP.Network.Packets.Game;

public class DamageFeedbackPacket : BasePacket
{
    public const PacketId PacketID = PacketId.DamageFeedback;

    public IdentifierData Target;
    public bool CausedByPlayer;

    public override PacketId TypeID => PacketID;
    public override Channel Channel => Channel.Player;

    public override void Send(Buffer writer)
    {
        writer.Write(Target);
        writer.Write(CausedByPlayer);
    }

    public override void Receive(Buffer reader)
    {
        Target = reader.ReadIdentifierData();
        CausedByPlayer = reader.ReadBoolean();
    }
}