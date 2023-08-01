using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace ThronefallMP.NetworkPackets;

public class EnemySpawnPacket : IPacket
{
    public const int PacketID = 6;
    
    public int Wave;
    public int Spawn;
    public int Id;
    public Vector3 Position;

    public int TypeID()
    {
        return PacketID;
    }

    public void Send(ref NetDataWriter writer)
    {
        writer.Put(Wave);
        writer.Put(Spawn);
        writer.Put(Id);
        writer.Put(Position.x);
        writer.Put(Position.y);
        writer.Put(Position.z);
    }

    public void Receive(ref NetPacketReader reader)
    {
        Wave = reader.GetInt();
        Spawn = reader.GetInt();
        Id = reader.GetInt();
        Position = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
    }
}