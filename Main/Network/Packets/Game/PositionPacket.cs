using Steamworks;
using ThronefallMP.Components;
using UnityEngine;

namespace ThronefallMP.Network.Packets.Game;

public class PositionPacket : BasePacket
{
    public const PacketId PacketID = PacketId.Position;

    public IdentifierData Target;
    public Vector3 Position;

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
        writer.Write(Position);
    }

    public override void Receive(Buffer reader)
    {
        Target.Type = (IdentifierType)reader.ReadInt32();
        Target.Id = reader.ReadInt32();
        Position = reader.ReadVector3();
    }
}