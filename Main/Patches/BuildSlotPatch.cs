using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using ThronefallMP.NetworkPackets;
using UnityEngine;
using UnityEngine.Serialization;

namespace ThronefallMP.Patches;

public static class BuildSlotPatch
{
    public static GameObject CoinPrefab;
    
    private static bool _disableNetworkHook = false; 
    
    public static void Apply()
    {
        On.BuildSlot.Start += Start;
        On.BuildSlot.OnUpgradeChoiceComplete += OnUpgradeChoiceComplete;
    }

    public static void HandleUpgrade(int id, int level, int choice)
    {
        var building = Identifier.GetGameObject(IdentifierType.Building, id).GetComponent<BuildSlot>();
        var upgrade = building.upgrades[level];
        var branch = upgrade.upgradeBranches[choice];
        var upgradeSelected = Traverse.Create(building).Field<BuildSlot.Upgrade>("upgradeSelected");
        upgradeSelected.Value = upgrade;
        _disableNetworkHook = true;
        building.OnUpgradeChoiceComplete(branch.choiceDetails);
        _disableNetworkHook = false;
        // TODO: CancelBuild if it is going.
    }

    private static void Start(On.BuildSlot.orig_Start original, BuildSlot self)
    {
        original(self);
        if (self.ActivatorBuilding == null)
        {
            self.StartCoroutine(ProcessBuildings(self));
        }
    }

    private static IEnumerator ProcessBuildings(BuildSlot root)
    {
        yield return new WaitForEndOfFrame();
        Identifier.Clear(IdentifierType.Building);
        Identifier.Clear(IdentifierType.Ally);
        
        Plugin.Log.LogInfo("Processing buildings");
        Plugin.Log.LogInfo("Added 1 for processing");
        var slots = new List<BuildSlot> { root };
        var buildingId = 0;
        var unitId = 0;
        while (slots.Count > 0)
        {
            var current = slots[0];
            slots.Remove(current);
            slots.AddRange(current.IsRootOf);
            AssignId(current, buildingId++);
            foreach (var respawn in current.GetComponentsInChildren<UnitRespawnerForBuildings>(true))
            {
                ProcessUnits(respawn, ref unitId);
            }
            
            if (current.IsRootOf.Count != 0)
            {
                Plugin.Log.LogInfo($"Added {current.IsRootOf.Count} for processing");
            }
        }
        
        Plugin.Log.LogInfo($"{buildingId} total buildings processed.");
        Plugin.Log.LogInfo($"{unitId} total units processed.");

        CoinPrefab = root.buildingInteractor.coinSpawner.coinPrefab;
    }

    private static void AssignId(BuildSlot self, int id)
    {
        var identifier = self.gameObject.AddComponent<Identifier>();
        identifier.SetIdentity(IdentifierType.Building, id);
        Plugin.Log.LogInfo("Building " + self.buildingName + " assigned id " + id);
    }

    private static void ProcessUnits(UnitRespawnerForBuildings respawn, ref int unitId)
    {
        Plugin.Log.LogInfo("Found respawner, processing units.");
        for (var i = 0; i < respawn.transform.childCount; ++i)
        {
            var unit = respawn.transform.GetChild(i);
            var identifier = unit.gameObject.AddComponent<Identifier>();
            Plugin.Log.LogInfo($"Unit {unit.name} assigned id {unitId}");
            identifier.SetIdentity(IdentifierType.Ally, unitId++);
        }
        
        Plugin.Log.LogInfo($"{respawn.transform.childCount} units processed.");
    }

    private static void OnUpgradeChoiceComplete(
        On.BuildSlot.orig_OnUpgradeChoiceComplete original,
        BuildSlot self,
        Choice choice)
    {
        if (!_disableNetworkHook)
        {
            var buildingId = self.GetComponent<Identifier>().Id;
            var upgradeSelected = Traverse.Create(self).Field<BuildSlot.Upgrade>("upgradeSelected");
            var upgradeIndex = 0;
            for (; upgradeIndex < self.upgrades.Count; ++upgradeIndex)
            {
                if (self.upgrades[upgradeIndex] == upgradeSelected.Value)
                {
                    break;
                }
            }

            var choiceIndex = 0;
            for (; choiceIndex < upgradeSelected.Value.upgradeBranches.Count; ++choiceIndex)
            {
                if (upgradeSelected.Value.upgradeBranches[choiceIndex].choiceDetails == choice)
                {
                    break;
                }
            }

            var packet = new BuildOrUpgradePacket
            {
                BuildingId = buildingId,
                Level = upgradeIndex,
                Choice = choiceIndex
            };

            Plugin.Instance.Network.Send(packet);
        }

        original(self, choice);
    }
}