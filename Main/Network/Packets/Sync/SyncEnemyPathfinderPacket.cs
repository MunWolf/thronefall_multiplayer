using System.Collections.Generic;
using Steamworks;
using ThronefallMP.Components;
using UnityEngine;

namespace ThronefallMP.Network.Packets.Sync;

public class SyncEnemyPathfinderPacket : BasePacket
{
    public const PacketId PacketID = PacketId.SyncEnemyPathfinder;
    public override PacketId TypeID => PacketID;
    public override Channel Channel => Channel.SyncUnit;
    public override int DeliveryMask => Constants.k_nSteamNetworkingSend_Reliable;

    public int Enemy;
    public IdentifierData TargetObject;
    public Vector3 Target;
    public bool WalkingHome;
    public bool ChasingPlayer;
    public bool Slowed;
    public int PathIndex;
    public List<Vector3> Path = new();
    
    public override void Send(Buffer writer)
    {
        writer.Write(Enemy);
        writer.Write(TargetObject);
        writer.Write(Target);
        writer.Write(WalkingHome);
        writer.Write(ChasingPlayer);
        writer.Write(Slowed);
        writer.Write(PathIndex);
        writer.Write(Path.Count);
        foreach (var point in Path)
        {
            writer.Write(point);
        }
    }

    public override void Receive(Buffer reader)
    {
        Path.Clear();
        Enemy = reader.ReadInt32();
        TargetObject = reader.ReadIdentifierData();
        Target = reader.ReadVector3();
        WalkingHome = reader.ReadBoolean();
        ChasingPlayer = reader.ReadBoolean();
        Slowed = reader.ReadBoolean();
        PathIndex = reader.ReadInt32();
        var count = reader.ReadInt32();
        for (var i = 0; i < count; ++i)
        {
             Path.Add(reader.ReadVector3());
        }
    }
}