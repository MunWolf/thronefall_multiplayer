using ThronefallMP.Components;
using ThronefallMP.Network.Packets.Game;
using UnityEngine;

namespace ThronefallMP.Patches;

public class UnitRespawnerForBuildingsPatch
{
    public static void Apply()
    {
        On.UnitRespawnerForBuildings.RespawnAKnockedOutUnit += RespawnAKnockedOutUnit;
    }

    private static void RespawnAKnockedOutUnit(On.UnitRespawnerForBuildings.orig_RespawnAKnockedOutUnit original, UnitRespawnerForBuildings self)
    {
        if (!Plugin.Instance.Network.Server)
        {
            return;
        }

        original(self);
    }
}