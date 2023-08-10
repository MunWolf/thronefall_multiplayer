
using Steamworks;
using ThronefallMP.Network;
using UnityEngine;

namespace ThronefallMP.NetworkPackets.Game;

public class EnemySpawnPacket : IPacket
{
    public const PacketId PacketID = PacketId.EnemySpawnPacket;
    
    public int Wave;
    public int Spawn;
    public int Id;
    public Vector3 Position;
    public int Coins;

    public PacketId TypeID => PacketID;
    public int DeliveryMask => Constants.k_nSteamNetworkingSend_Reliable;
    public int Channel => 0;

    public void Send(Buffer writer)
    {
        writer.Write(Wave);
        writer.Write(Spawn);
        writer.Write(Id);
        writer.Write(Position);
        writer.Write(Coins);
    }

    public void Receive(Buffer reader)
    {
        Wave = reader.ReadInt32();
        Spawn = reader.ReadInt32();
        Id = reader.ReadInt32();
        Position = reader.ReadVector3();
        Coins = reader.ReadInt32();
    }
}