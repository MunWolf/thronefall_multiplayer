using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using ThronefallMP.Components;
using ThronefallMP.Network.Packets.PlayerCommand;
using UnityEngine;

namespace ThronefallMP.Patches;

public static class BuildSlotPatch
{
    public struct UpgradeInfo
    {
        public int Cost;
        public int CurrentLevel;
    }
    
    public static GameObject CoinPrefab;
    
    private static bool _disableNetworkHook; 
    
    public static void Apply()
    {
        On.BuildSlot.Start += Start;
        On.BuildSlot.OnUpgradeChoiceComplete += OnUpgradeChoiceComplete;
    }

    private static void Start(On.BuildSlot.orig_Start original, BuildSlot self)
    {
        original(self);
        if (self.ActivatorBuilding == null)
        {
            // TODO: Maybe change this to happen on SceneManager sceneLoaded instead?
            self.StartCoroutine(ProcessBuildings(self));
        }
    }

    private static IEnumerator ProcessBuildings(BuildSlot root)
    {
        yield return new WaitForEndOfFrame();
        Identifier.Clear(IdentifierType.BuildSlot);
        Identifier.Clear(IdentifierType.Building);
        Identifier.Clear(IdentifierType.Ally);
        
        Plugin.Log.LogInfo("Processing buildings");
        var slots = new List<BuildSlot> { root };
        while (slots.Count > 0)
        {
            var current = slots[0];
            slots.Remove(current);
            slots.AddRange(current.IsRootOf);
            AssignId(current, Helpers.GetPath(current.transform).GetHashCode());
            foreach (var respawn in current.GetComponentsInChildren<UnitRespawnerForBuildings>(true))
            {
                ProcessUnits(respawn);
            }
        }

        CoinPrefab = root.buildingInteractor.coinSpawner.coinPrefab;
    }

    private static void AssignId(BuildSlot self, int id)
    {
        {
            var identifier = self.gameObject.AddComponent<Identifier>();
            identifier.SetIdentity(IdentifierType.BuildSlot, id);
        }
        {
            var building = self.GetComponentInChildren<Hp>(true);
            var identifier = building.gameObject.AddComponent<Identifier>();
            identifier.SetIdentity(IdentifierType.Building, id);
        }
    }

    private static void ProcessUnits(UnitRespawnerForBuildings respawn)
    {
        for (var i = 0; i < respawn.transform.childCount; ++i)
        {
            var unit = respawn.transform.GetChild(i);
            var identifier = unit.gameObject.AddComponent<Identifier>();
            var id = Helpers.GetPath(unit).GetHashCode();
            identifier.SetIdentity(IdentifierType.Ally, id);
        }
    }

    private static void OnUpgradeChoiceComplete(
        On.BuildSlot.orig_OnUpgradeChoiceComplete original,
        BuildSlot self,
        Choice choice)
    {
        var upgradeSelected = Traverse.Create(self).Field<BuildSlot.Upgrade>("upgradeSelected").Value;
        if (upgradeSelected == null)
        {
            // We happened to get here because we finished our choice after handling a ConfirmBuildPacket
            return;
        }
        
        if (!_disableNetworkHook && choice != null)
        {
            var buildingId = self.GetComponent<Identifier>().Id;
            var upgradeIndex = 0;
            for (; upgradeIndex < self.upgrades.Count; ++upgradeIndex)
            {
                if (self.upgrades[upgradeIndex] == upgradeSelected)
                {
                    break;
                }
            }

            var choiceIndex = 0;
            for (; choiceIndex < upgradeSelected.upgradeBranches.Count; ++choiceIndex)
            {
                if (upgradeSelected.upgradeBranches[choiceIndex].choiceDetails == choice)
                {
                    break;
                }
            }

            if (Plugin.Instance.Network.Server)
            {
                var packet = new ConfirmBuildPacket()
                {
                    BuildingId = buildingId,
                    Level = upgradeIndex,
                    Choice = choiceIndex,
                    PlayerID = Plugin.Instance.PlayerManager.LocalId
                };
                
                Plugin.Instance.Network.Send(packet, true);
            }
            else
            {
                var packet = new BuildOrUpgradePacket
                {
                    BuildingId = buildingId,
                    Level = upgradeIndex,
                    Choice = choiceIndex
                };

                Plugin.Instance.Network.Send(packet);
            }
        }
        else
        {
            original(self, choice);
        }
    }
    
    public static UpgradeInfo GetUpgradeInfo(int id, int level, int choice)
    {
        var building = Identifier.GetGameObject(IdentifierType.BuildSlot, id).GetComponent<BuildSlot>();
        var upgrade = building.upgrades[level];
        return new UpgradeInfo()
        {
            Cost = upgrade.cost,
            CurrentLevel = building.Level
        };
    }

    public static void HandleUpgrade(int playerId, int id, int level, int choice)
    {
        var building = Identifier.GetGameObject(IdentifierType.BuildSlot, id).GetComponent<BuildSlot>();
        if (building == null)
        {
            Plugin.Log.LogInfo($"Unable to build {id}:{level}:{choice} for {playerId}");
        }

        if (building.buildingInteractor == null)
        {
            Plugin.Log.LogInfo($"Building interactor for {id}:{level}:{choice} inactive");
            building.buildingInteractor = building.GetComponentInChildren<BuildingInteractor>(true);
        }
        
        var upgrade = building.upgrades[level];
        var branch = upgrade.upgradeBranches[choice];
        var upgradeSelected = Traverse.Create(building).Field<BuildSlot.Upgrade>("upgradeSelected");
        upgradeSelected.Value = upgrade;
        _disableNetworkHook = true;
        building.buildingInteractor.MarkAsHarvested();
        building.OnUpgradeChoiceComplete(branch.choiceDetails);
        _disableNetworkHook = false;
        upgradeSelected.Value = null;
        
        var focussed = Traverse.Create(building.buildingInteractor).Field<bool>("focussed");
        if (building.buildingInteractor.IsWaitingForChoice)
        {
            Plugin.Log.LogInfo("Cancel choice");
            // We are waiting on choice when the building has already been built, cancel it.
            UIFrameManager.instance.CloseActiveFrame();
            ChoiceManager.instance.CancelChoice();
        }
        else switch (focussed.Value)
        {
            case true when playerId != Plugin.Instance.PlayerManager.LocalId:
            {
                Plugin.Log.LogInfo("Redo our focus");
                //Traverse.Create(building.buildingInteractor).Field<bool>("isWaitingForChoice").Value = true;
                //building.OnUpgradeChoiceComplete(null);
                var player = Plugin.Instance.PlayerManager.LocalPlayer.Object.GetComponent<PlayerInteraction>();
                building.buildingInteractor.Unfocus(player);
                break;
            }
            case true:
            {
                Traverse.Create(building.buildingInteractor).Method("BuildComplete").GetValue();
                break;
            }
        }
    }

    public static void CancelBuild(int id)
    {
        var building = Identifier.GetGameObject(IdentifierType.BuildSlot, id).GetComponent<BuildSlot>();
        building.OnUpgradeChoiceComplete(null);
    }
}