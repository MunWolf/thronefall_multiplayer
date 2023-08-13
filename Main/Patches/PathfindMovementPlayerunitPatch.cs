using HarmonyLib;
using Pathfinding;
using ThronefallMP.Components;
using ThronefallMP.Network.Packets.Game;
using ThronefallMP.Network.Packets.Sync;
using UnityEngine;

namespace ThronefallMP.Patches;

public static class PathfindMovementPlayerunitPatch
{
    public static void Apply()
    {
        On.PathfindMovementPlayerunit.OriginalOnPathComplete += OriginalOnPathComplete;
        On.PathfindMovementPlayerunit.BackupOnPathComplete += BackupOnPathComplete;
        On.PathfindMovementPlayerunit.FindMoveToTarget += FindMoveToTarget;
    }

    private static void OriginalOnPathComplete(On.PathfindMovementPlayerunit.orig_OriginalOnPathComplete original, PathfindMovementPlayerunit self, Path p)
    {
        if (Plugin.Instance.Network.Server)
        {
            original(self, p);
        }
    }

    private static void BackupOnPathComplete(On.PathfindMovementPlayerunit.orig_BackupOnPathComplete original, PathfindMovementPlayerunit self, Path p)
    {
        if (Plugin.Instance.Network.Server)
        {
            original(self, p);
        }
    }

    private static Vector3 FindMoveToTarget(On.PathfindMovementPlayerunit.orig_FindMoveToTarget original, PathfindMovementPlayerunit self)
    {
        return Plugin.Instance.Network.Server
            ? original(self)
            : Traverse.Create(self).Field<Vector3>("seekToTargetPos").Value;
    }
}