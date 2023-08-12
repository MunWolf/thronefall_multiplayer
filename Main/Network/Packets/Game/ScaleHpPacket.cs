using Steamworks;
using ThronefallMP.Components;

namespace ThronefallMP.Network.Packets.Game;

public class ScaleHpPacket : BasePacket
{
    public const PacketId PacketID = PacketId.ScaleHp;

    public IdentifierData Target;
    public float Multiplier;

    public override PacketId TypeID => PacketID;
    public override Channel Channel => Channel.Player;
    public override bool CanHandle(CSteamID sender)
    {
        return Plugin.Instance.Network.IsServer(sender);
    }

    public override void Send(Buffer writer)
    {
        writer.Write((int)Target.Type);
        writer.Write(Target.Id);
        writer.Write(Multiplier);
    }

    public override void Receive(Buffer reader)
    {
        Target.Type = (IdentifierType)reader.ReadInt32();
        Target.Id = reader.ReadInt32();
        Multiplier = reader.ReadFloat();
    }
}