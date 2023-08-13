using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Steamworks;
using ThronefallMP.Components;
using ThronefallMP.Network.Packets;
using ThronefallMP.Network.Packets.Sync;
using UnityEngine;

namespace ThronefallMP.Network.Sync;

public class AllyPathfinderSync : BaseTargetSync
{
    protected override IEnumerable<(IdentifierData id, GameObject target)> Targets()
    {
        foreach (var data in Identifier.GetIdentifiers(IdentifierType.Ally))
        {
            yield return (new IdentifierData { Type = IdentifierType.Ally, Id = data.id }, data.target);
        }
    }

    protected override BasePacket CreateSyncPacket(CSteamID peer, IdentifierData id, GameObject target)
    {
        var pathfinder = target.GetComponent<PathfindMovementPlayerunit>();
        // We should send nextPathPointIndex but not check it in the hash.
        var targetPosition = Traverse.Create(pathfinder).Field<Vector3>("seekToTargetPos");
        var walkingHome = Traverse.Create(pathfinder).Field<bool>("currentlyWalkingHome");
        var followingPlayer = Traverse.Create(pathfinder).Field<bool>("followingPlayer");
        var nextPathPointIndex = Traverse.Create(pathfinder).Field<int>("nextPathPointIndex");
        var path = Traverse.Create(pathfinder).Field<List<Vector3>>("path");
        return new SyncAllyPathfinderPacket
        {
            Ally = id.Id,
            Target = targetPosition.Value,
            WalkingHome = walkingHome.Value,
            FollowingPlayer = followingPlayer.Value,
            Slowed = pathfinder.IsSlowed,
            PathIndex = nextPathPointIndex.Value,
            Path = path.Value
        };
    }

    protected override bool Compare(CSteamID peer, IdentifierData id, GameObject target, BasePacket current, BasePacket last)
    {
        var a = (SyncAllyPathfinderPacket)current;
        var b = (SyncAllyPathfinderPacket)last;

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
               && a.WalkingHome == b.WalkingHome
               && a.FollowingPlayer == b.FollowingPlayer
               && a.Slowed == b.Slowed;
    }

    public override bool CanHandle(BasePacket packet)
    {
        return packet.TypeID == SyncAllyPathfinderPacket.PacketID;
    }

    public override void Handle(CSteamID peer, BasePacket packet)
    {
        var sync = (SyncAllyPathfinderPacket)packet;
        var enemy = Identifier.GetGameObject(IdentifierType.Enemy, sync.Ally);
        if (enemy == null)
        {
            Plugin.Log.LogInfoFiltered("PathfindingEnemySync", $"Ally {sync.Ally} not found, discarding");
            return;
        }
        
        var pathfinder = enemy.GetComponent<PathfindMovementPlayerunit>();
        var targetPosition = Traverse.Create(pathfinder).Field<Vector3>("seekToTargetPos");
        var walkingHome = Traverse.Create(pathfinder).Field<bool>("currentlyWalkingHome");
        var followingPlayer = Traverse.Create(pathfinder).Field<bool>("followingPlayer");
        var slowedFor = Traverse.Create(pathfinder).Field<float>("slowedFor");
        var nextPathPointIndex = Traverse.Create(pathfinder).Field<int>("nextPathPointIndex");
        var path = Traverse.Create(pathfinder).Field<List<Vector3>>("path");
        targetPosition.Value = sync.Target;
        walkingHome.Value = sync.WalkingHome;
        followingPlayer.Value = sync.FollowingPlayer;
        slowedFor.Value = sync.Slowed ? float.MaxValue : float.MinValue;
        nextPathPointIndex.Value = sync.PathIndex;
        path.Value = sync.Path;
    }
}