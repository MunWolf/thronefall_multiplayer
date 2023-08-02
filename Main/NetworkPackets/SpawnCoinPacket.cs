using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace ThronefallMP.NetworkPackets;

public class SpawnCoinPacket : IPacket
{
    public const PacketId PacketID = PacketId.SpawnCoinPacket;

    public int Player;
    public Vector3 Position;
    public Quaternion Rotation;
    
    public PacketId TypeID()
    {
        return PacketID;
    }

    public void Send(ref NetDataWriter writer)
    {
        writer.Put(Player);
        writer.Put(Position);
        writer.Put(Rotation);
    }

    public void Receive(ref NetPacketReader reader)
    {
        Player = reader.GetInt();
        Position = reader.GetVector3();
        Rotation = reader.GetQuaternion();
    }
}