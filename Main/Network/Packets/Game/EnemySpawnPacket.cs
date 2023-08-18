using Steamworks;
using UnityEngine;

namespace ThronefallMP.Network.Packets.Game;

public class EnemySpawnPacket : BasePacket
{
    public const PacketId PacketID = PacketId.EnemySpawn;
    
    public byte Wave;
    public byte Spawn;
    public ushort Id;
    public Vector3 Position;
    public byte Coins;

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
        Wave = reader.ReadByte();
        Spawn = reader.ReadByte();
        Id = reader.ReadUInt16();
        Position = reader.ReadVector3();
        Coins = reader.ReadByte();
    }
}