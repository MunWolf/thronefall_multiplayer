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
    public class Identifier : MonoBehaviour
    {
        public int id;
    }

    private static readonly Dictionary<int, BuildSlot> Buildings = new();
    
    private static bool _disableNetworkHook = false; 
    
    public static void Apply()
    {
        On.BuildSlot.Start += Start;
        On.BuildSlot.OnUpgradeChoiceComplete += OnUpgradeChoiceComplete;
    }

    public static void HandleUpgrade(int id, int level, int choice)
    {
        var building = Buildings[id];
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
        Plugin.Log.LogInfo("Processing buildings");
        Plugin.Log.LogInfo("Added " + 1 + " for processing");
        var slots = new List<BuildSlot> { root };
        var id = 0;
        while (slots.Count > 0)
        {
            var current = slots[0];
            slots.Remove(current);
            slots.AddRange(current.IsRootOf);
            AssignId(current, id++);
            Plugin.Log.LogInfo("Added " + current.BuiltSlotsThatRelyOnThisBuilding.Count + " for processing");
        }
        
        Plugin.Log.LogInfo(Buildings.Count + " buildings processed.");
    }

    private static void AssignId(BuildSlot self, int id)
    {
        var identifier = self.gameObject.AddComponent<Identifier>();
        identifier.id = id;
        Buildings[id] = self;
        Plugin.Log.LogInfo("Building " + self.buildingName + " assigned id " + id);
    }

    private static void OnUpgradeChoiceComplete(
        On.BuildSlot.orig_OnUpgradeChoiceComplete original,
        BuildSlot self,
        Choice choice)
    {
        if (!_disableNetworkHook)
        {
            var buildingId = self.GetComponent<Identifier>().id;
            
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