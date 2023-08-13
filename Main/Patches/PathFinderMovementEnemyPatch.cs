using HarmonyLib;
using Pathfinding;
using ThronefallMP.Components;
using ThronefallMP.Network.Packets.Game;
using UnityEngine;

namespace ThronefallMP.Patches;

public static class PathFinderMovementEnemyPatch
{
    public static void Apply()
    {
        On.PathfindMovementEnemy.OriginalOnPathComplete += OriginalOnPathComplete;
        On.PathfindMovementEnemy.BackupOnPathComplete += BackupOnPathComplete;
        On.PathfindMovementEnemy.FindMoveToTarget += FindMoveToTarget;
    }

    // TODO: FollowPathUpdate uses CalculateMovementDeltaTime which depends on these, maybe look into just noopint the OnComplete instead?
    private static void OriginalOnPathComplete(On.PathfindMovementEnemy.orig_OriginalOnPathComplete original, PathfindMovementEnemy self, Path p)
    {
        // We sync all path requests from the server.
        if (Plugin.Instance.Network.Server)
        {
            original(self, p);
        }
    }

    private static void BackupOnPathComplete(On.PathfindMovementEnemy.orig_BackupOnPathComplete original, PathfindMovementEnemy self, Path p)
    {
        // We sync all path requests from the server.
        if (Plugin.Instance.Network.Server)
        {
            original(self, p);
        }
    }

    private static Vector3 FindMoveToTarget(On.PathfindMovementEnemy.orig_FindMoveToTarget original, PathfindMovementEnemy self)
    {
        if (Plugin.Instance.Network.Server)
        {
            return original(self);
        }

        return Traverse.Create(self).Field<Vector3>("seekToTargetPos").Value;
    }
}