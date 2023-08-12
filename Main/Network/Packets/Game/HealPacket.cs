using Steamworks;
using ThronefallMP.Components;

namespace ThronefallMP.Network.Packets.Game;

public class HealPacket : BasePacket
{
    public const PacketId PacketID = PacketId.Heal;

    public IdentifierData Target;
    public float Amount;

    public override PacketId TypeID => PacketID;
    public override Channel Channel => Channel.Player;
    public override bool CanHandle(CSteamID sender)
    {
        return Plugin.Instance.Network.IsServer(sender);
    }

    public override void Send(Buffer writer)
    {
        writer.Write(Target);
        writer.Write(Amount);
    }

    public override void Receive(Buffer reader)
    {
        Target = reader.ReadIdentifierData();
        Amount = reader.ReadFloat();
    }
}