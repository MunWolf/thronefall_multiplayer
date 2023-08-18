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
        var nextPathPointIndex = Traverse.Create(pathfinder).Field<int>("nextPathPointIndex");
        var path = Traverse.Create(pathfinder).Field<List<Vector3>>("path");
        var followingPlayer = Traverse.Create(pathfinder).Field<bool>("followingPlayer");
        var seekToTaggedObj = Traverse.Create(pathfinder).Field<TaggedObject>("seekToTaggedObj");
        var seekToTaggedObjId = IdentifierData.Invalid;
        if (seekToTaggedObj.Value != null)
        {
            seekToTaggedObjId = new IdentifierData(seekToTaggedObj.Value.GetComponent<Identifier>());
        }
        else if (followingPlayer.Value)
        {
            // We don't care which player because it sets it to null anyway.
            seekToTaggedObjId = new IdentifierData { Type = IdentifierType.Player };
        }
        
        return new SyncAllyPathfinderPacket
        {
            Ally = id.Id,
            TargetObject = seekToTaggedObjId,
            HomePosition = pathfinder.HomePosition,
            HoldPosition = pathfinder.HoldPosition,
            HasReachedHomePositionAlready = pathfinder.HasReachedHomePositionAlready,
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

        return a.TargetObject.Type == b.TargetObject.Type
               && a.TargetObject.Id == b.TargetObject.Id
               && a.HomePosition == b.HomePosition
               && a.HoldPosition == b.HoldPosition
               && a.HasReachedHomePositionAlready == b.HasReachedHomePositionAlready
               && a.Slowed == b.Slowed;
    }

    public override bool CanHandle(BasePacket packet)
    {
        return packet.TypeID == SyncAllyPathfinderPacket.PacketID;
    }

    public override void Handle(CSteamID peer, BasePacket packet)
    {
        var sync = (SyncAllyPathfinderPacket)packet;
        var ally = Identifier.GetGameObject(IdentifierType.Ally, sync.Ally);
        if (ally == null)
        {
            Plugin.Log.LogInfoFiltered("PathfindingAllySync", $"Ally {sync.Ally} not found, discarding");
            return;
        }
        
        var pathfinder = ally.GetComponent<PathfindMovementPlayerunit>();
        var targetPosition = Traverse.Create(pathfinder).Field<Vector3>("seekToTargetPos");
        var walkingHome = Traverse.Create(pathfinder).Field<bool>("currentlyWalkingHome");
        var hasReachedHomePositionAlready = Traverse.Create(pathfinder).Field<bool>("hasReachedHomePositionAlready");
        var followingPlayer = Traverse.Create(pathfinder).Field<bool>("followingPlayer");
        var slowedFor = Traverse.Create(pathfinder).Field<float>("slowedFor");
        var nextPathPointIndex = Traverse.Create(pathfinder).Field<int>("nextPathPointIndex");
        var path = Traverse.Create(pathfinder).Field<List<Vector3>>("path");
        var seekToTaggedObj = Traverse.Create(pathfinder).Field<TaggedObject>("seekToTaggedObj");
        var target = sync.TargetObject.Get();
        var isTargetNull = sync.TargetObject.Type == IdentifierType.Invalid;
        
        pathfinder.HomePosition = sync.HomePosition;
        pathfinder.HoldPosition = sync.HoldPosition;
        followingPlayer.Value = sync.TargetObject.Type == IdentifierType.Player;
        targetPosition.Value = followingPlayer.Value || walkingHome.Value || target == null ? pathfinder.HomePosition : target.transform.position;
        seekToTaggedObj.Value = isTargetNull || sync.TargetObject.Type == IdentifierType.Player ? null : target.GetComponent<TaggedObject>();
        walkingHome.Value = isTargetNull;
        hasReachedHomePositionAlready.Value = sync.HasReachedHomePositionAlready;
        slowedFor.Value = sync.Slowed ? float.MaxValue : float.MinValue;
        nextPathPointIndex.Value = sync.PathIndex;
        path.Value = sync.Path;
    }
}