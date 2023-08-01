using System.Collections.Generic;
using HarmonyLib;

namespace ThronefallMP.Patches;

public static class BuildingInteractorPatch
{
    public static void Apply()
    {
        On.BuildingInteractor.OnDawn_AfterSunrise += OnDawn_AfterSunrise;
    }

    private static void OnDawn_AfterSunrise(On.BuildingInteractor.orig_OnDawn_AfterSunrise original, BuildingInteractor self)
    {
        var incomeModifiers = Traverse.Create(self).Field<List<IncomeModifyer>>("incomeModifiers");
        // TODO: Make coins go to the closest player instead of this.
        var component = Plugin.Instance.Network.GetPlayerData(-1).GetComponent<PlayerInteraction>();
        self.Harvest(component);
        foreach (IncomeModifyer incomeModifyer in incomeModifiers.Value)
        {
            incomeModifyer.OnDawn();
        }
        
        self.UpdateInteractionState(false, BuildingInteractor.InteractionState.None);
    }
}