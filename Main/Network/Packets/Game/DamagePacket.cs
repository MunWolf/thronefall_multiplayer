using Steamworks;
using ThronefallMP.Components;

namespace ThronefallMP.Network.Packets.Game;

public class DamagePacket : BasePacket
{
    public const PacketId PacketID = PacketId.Damage;

    public IdentifierData Target;
    public IdentifierData Source;
    public float Damage;
    public bool CausedByPlayer;
    public bool InvokeFeedbackEvents;

    public override PacketId TypeID => PacketID;
    public override Channel Channel => Channel.Player;
    public override bool CanHandle(CSteamID sender)
    {
        return Plugin.Instance.Network.IsServer(sender);
    }

    public override void Send(Buffer writer)
    {
        writer.Write(Target);
        writer.Write(Source);
        writer.Write(Damage);
        writer.Write(CausedByPlayer);
        writer.Write(InvokeFeedbackEvents);
    }

    public override void Receive(Buffer reader)
    {
        Target = reader.ReadIdentifierData();
        Source = reader.ReadIdentifierData();
        Damage = reader.ReadFloat();
        CausedByPlayer = reader.ReadBoolean();
        InvokeFeedbackEvents = reader.ReadBoolean();
    }
}