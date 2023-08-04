using LiteNetLib;
using LiteNetLib.Utils;
using ThronefallMP.Network;
using UnityEngine;

namespace ThronefallMP.NetworkPackets;

public class EnemySpawnPacket : IPacket
{
    public const PacketId PacketID = PacketId.EnemySpawnPacket;
    
    public int Wave;
    public int Spawn;
    public int Id;
    public Vector3 Position;
    public int Coins;

    public PacketId TypeID()
    {
        return PacketID;
    }

    public void Send(ref NetDataWriter writer)
    {
        writer.Put(Wave);
        writer.Put(Spawn);
        writer.Put(Id);
        writer.Put(Position);
        writer.Put(Coins);
    }

    public void Receive(ref NetPacketReader reader)
    {
        Wave = reader.GetInt();
        Spawn = reader.GetInt();
        Id = reader.GetInt();
        Position = reader.GetVector3();
        Coins = reader.GetInt();
    }
}