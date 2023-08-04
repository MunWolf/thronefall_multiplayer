using Lidgren.Network;
using ThronefallMP.Network;
using UnityEngine;

namespace ThronefallMP.NetworkPackets.Game;

public class SpawnCoinPacket : IPacket
{
    public const PacketId PacketID = PacketId.SpawnCoinPacket;

    public int Player;
    public Vector3 Position;
    public Quaternion Rotation;
    
    public PacketId TypeID => PacketID;
    public NetDeliveryMethod Delivery => NetDeliveryMethod.ReliableOrdered;
    public int Channel => 0;
    
    public void Send(NetBuffer writer)
    {
        writer.Write(Player);
        writer.Write(Position);
        writer.Write(Rotation);
    }

    public void Receive(NetBuffer reader)
    {
        Player = reader.ReadInt32();
        Position = reader.ReadVector3();
        Rotation = reader.ReadQuaternion();
    }
}