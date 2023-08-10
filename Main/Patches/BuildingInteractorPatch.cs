using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace ThronefallMP.Patches;

public static class BuildingInteractorPatch
{
    public static void Apply()
    {
        On.BuildingInteractor.OnDawn_AfterSunrise += OnDawn_AfterSunrise;
        On.BuildingInteractor.InteractionHold += InteractionHold;
    }

    private static void OnDawn_AfterSunrise(On.BuildingInteractor.orig_OnDawn_AfterSunrise original, BuildingInteractor self)
    {
        var incomeModifiers = Traverse.Create(self).Field<List<IncomeModifyer>>("incomeModifiers");
        var players = Plugin.Instance.PlayerManager.GetAllPlayerData().Select(x => x.GetComponent<PlayerInteraction>()).ToArray();
        var closest = Utils.FindClosest(players, self.transform.position);
        self.Harvest(closest);
        foreach (var modifier in incomeModifiers.Value)
        {
            modifier.OnDawn();
        }
        
        self.UpdateInteractionState();
    }

    private static void InteractionHold(On.BuildingInteractor.orig_InteractionHold original, BuildingInteractor self, PlayerInteraction player)
    {
        Traverse.Create(player).Field<int>("balance").Value = GlobalData.Balance;
        original(self, player);
    }
}