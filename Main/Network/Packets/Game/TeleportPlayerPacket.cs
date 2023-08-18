using UnityEngine;

namespace ThronefallMP.Network.Packets.Game;

public class TeleportPlayerPacket : BasePacket
{
    public const PacketId PacketID = PacketId.TeleportPlayer;

    public override PacketId TypeID => PacketID;
    public override Channel Channel => Channel.SyncPositions;
    public override bool ShouldPropagate => true;

    public ushort PlayerId;
    public Vector3 Position;
    
    public override void Send(Buffer writer)
    {
        writer.Write(PlayerId);
        writer.Write(Position);
    }

    public override void Receive(Buffer reader)
    {
        PlayerId = reader.ReadUInt16();
        Position = reader.ReadVector3();
    }
}