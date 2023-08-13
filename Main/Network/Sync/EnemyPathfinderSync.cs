using System.Collections.Generic;
using HarmonyLib;
using Steamworks;
using ThronefallMP.Components;
using ThronefallMP.Network.Packets;
using ThronefallMP.Network.Packets.Sync;
using UnityEngine;

namespace ThronefallMP.Network.Sync;

public class EnemyPathfinderSync : BaseTargetSync
{
    protected override IEnumerable<(IdentifierData id, GameObject target)> Targets()
    {
        foreach (var data in Identifier.GetIdentifiers(IdentifierType.Enemy))
        {
            yield return (new IdentifierData { Type = IdentifierType.Enemy, Id = data.id }, data.target);
        }
    }

    protected override BasePacket CreateSyncPacket(CSteamID peer, IdentifierData id, GameObject target)
    {
        var pathfinder = target.GetComponent<PathfindMovementEnemy>();
        // We should send nextPathPointIndex but not check it in the hash.
        var targetPosition = Traverse.Create(pathfinder).Field<Vector3>("seekToTargetPos");
        var seekToTaggedObj = Traverse.Create(pathfinder).Field<TaggedObject>("seekToTaggedObj");
        var walkingHome = Traverse.Create(pathfinder).Field<bool>("currentlyWalkingHome");
        var chasingPlayer = Traverse.Create(pathfinder).Field<bool>("currentlyChasingPlayer");
        var nextPathPointIndex = Traverse.Create(pathfinder).Field<int>("nextPathPointIndex");
        var path = Traverse.Create(pathfinder).Field<List<Vector3>>("path");

        var seekToTaggedObjId = IdentifierData.Invalid;
        if (seekToTaggedObj.Value != null)
        {
            seekToTaggedObjId = new IdentifierData(seekToTaggedObj.Value.GetComponent<Identifier>());
        }
        
        return new SyncEnemyPathfinderPacket
        {
            Enemy = id.Id,
            TargetObject = seekToTaggedObjId,
            Target = targetPosition.Value,
            WalkingHome = walkingHome.Value,
            ChasingPlayer = chasingPlayer.Value,
            Slowed = pathfinder.IsSlowed,
            PathIndex = nextPathPointIndex.Value,
            Path = path.Value
        };
    }

    protected override bool Compare(CSteamID peer, IdentifierData id, GameObject target, BasePacket current, BasePacket last)
    {
        var a = (SyncEnemyPathfinderPacket)current;
        var b = (SyncEnemyPathfinderPacket)last;

        if (a.Path.Count != b.Path.Count)
        {
            return false;
        }

        for (var i = 0; i < a.Path.Count; ++i)
        {
            if ((a.Path[i] - b.Path[i]).sqrMagnitude < Helpers.EpsilonSqr)
            {
                return false;
            }
        }

        return (a.Target - b.Target).sqrMagnitude < Helpers.EpsilonSqr
               && a.TargetObject.Type == b.TargetObject.Type
               && a.TargetObject.Id == b.TargetObject.Id
               && a.WalkingHome == b.WalkingHome
               && a.ChasingPlayer == b.ChasingPlayer
               && a.Slowed == b.Slowed;
    }

    public override bool CanHandle(BasePacket packet)
    {
        return packet.TypeID == SyncEnemyPathfinderPacket.PacketID;
    }

    public override void Handle(CSteamID peer, BasePacket packet)
    {
        var sync = (SyncEnemyPathfinderPacket)packet;
        var enemy = Identifier.GetGameObject(IdentifierType.Enemy, sync.Enemy);
        if (enemy == null)
        {
            Plugin.Log.LogInfoFiltered("PathfindingEnemySync", $"Enemy {sync.Enemy} not found, discarding");
            return;
        }
        
        var pathfinder = enemy.GetComponent<PathfindMovementEnemy>();
        var targetPosition = Traverse.Create(pathfinder).Field<Vector3>("seekToTargetPos");
        var seekToTaggedObj = Traverse.Create(pathfinder).Field<TaggedObject>("seekToTaggedObj");
        var walkingHome = Traverse.Create(pathfinder).Field<bool>("currentlyWalkingHome");
        var chasingPlayer = Traverse.Create(pathfinder).Field<bool>("currentlyChasingPlayer");
        var slowedFor = Traverse.Create(pathfinder).Field<float>("slowedFor");
        var nextPathPointIndex = Traverse.Create(pathfinder).Field<int>("nextPathPointIndex");
        var path = Traverse.Create(pathfinder).Field<List<Vector3>>("path");
        var target = sync.TargetObject.Get();
        targetPosition.Value = sync.Target;
        seekToTaggedObj.Value = target != null ? target.GetComponent<TaggedObject>() : null;
        walkingHome.Value = sync.WalkingHome;
        chasingPlayer.Value = sync.ChasingPlayer;
        slowedFor.Value = sync.Slowed ? float.MaxValue : float.MinValue;
        nextPathPointIndex.Value = sync.PathIndex;
        path.Value = sync.Path;
    }
}