using Steamworks;
using ThronefallMP.Components;
using UnityEngine;

namespace ThronefallMP.Network.Packets.Sync;

public class SyncPositionPacket : BasePacket
{
    public const PacketId PacketID = PacketId.SyncPosition;

    public IdentifierData Target;
    public Vector3 Position;

    public override PacketId TypeID => PacketID;

    public override Channel Channel => Channel.SyncPositions;

    public override void Send(Buffer writer)
    {
        writer.Write(Target);
        writer.Write(Position);
    }

    public override void Receive(Buffer reader)
    {
        Target = reader.ReadIdentifierData();
        Position = reader.ReadVector3();
    }
}