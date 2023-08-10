using Steamworks;
using ThronefallMP.Components;
using ThronefallMP.Network;
using UnityEngine;

namespace ThronefallMP.NetworkPackets.Game;

public class PositionPacket : IPacket
{
    public const PacketId PacketID = PacketId.PositionPacket;

    public IdentifierData Target;
    public Vector3 Position;

    public PacketId TypeID => PacketID;
    public int DeliveryMask => Constants.k_nSteamNetworkingSend_Reliable;
    public int Channel => 0;

    public void Send(Buffer writer)
    {
        writer.Write((int)Target.Type);
        writer.Write(Target.Id);
        writer.Write(Position);
    }

    public void Receive(Buffer reader)
    {
        Target.Type = (IdentifierType)reader.ReadInt32();
        Target.Id = reader.ReadInt32();
        Position = reader.ReadVector3();
    }
}