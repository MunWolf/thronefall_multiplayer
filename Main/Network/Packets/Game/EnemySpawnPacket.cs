using Steamworks;
using UnityEngine;

namespace ThronefallMP.Network.Packets.Game;

public class EnemySpawnPacket : BasePacket
{
    public const PacketId PacketID = PacketId.EnemySpawn;
    
    public int Wave;
    public int Spawn;
    public int Id;
    public Vector3 Position;
    public int Coins;

    public override PacketId TypeID => PacketID;
    public override Channel Channel => Channel.Game;
    public override bool CanHandle(CSteamID sender)
    {
        return Plugin.Instance.Network.IsServer(sender);
    }

    public override void Send(Buffer writer)
    {
        writer.Write(Wave);
        writer.Write(Spawn);
        writer.Write(Id);
        writer.Write(Position);
        writer.Write(Coins);
    }

    public override void Receive(Buffer reader)
    {
        Wave = reader.ReadInt32();
        Spawn = reader.ReadInt32();
        Id = reader.ReadInt32();
        Position = reader.ReadVector3();
        Coins = reader.ReadInt32();
    }
}