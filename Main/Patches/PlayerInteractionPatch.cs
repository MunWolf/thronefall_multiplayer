using HarmonyLib;
using UnityEngine;

namespace ThronefallMP.Patches;

public static class PlayerInteractionPatch
{
    public static void Apply()
    {
        On.PlayerInteraction.FetchCoins += FetchCoins;
        On.PlayerInteraction.FetchInteractors += FetchInteractors;
        On.PlayerInteraction.RunInteraction += RunInteraction;
    }

    private static void FetchCoins(On.PlayerInteraction.orig_FetchCoins original, PlayerInteraction self)
    {
        Collider[] array = Physics.OverlapSphere(self.transform.position, self.coinMagnetRadius, self.coinLayer);
        for (int i = 0; i < array.Length; i++)
        {
            Coin component = array[i].GetComponent<Coin>();
            if (component != null && component.IsFree)
            {
                var target = Traverse.Create(component).Field<PlayerInteraction>("target");
                if (target.Value == null || target.Value == self)
                {
                    component.SetTarget(self);
                }
                else
                {
                    var coinPosition = component.transform.position;
                    var targetDistance = (target.Value.transform.position - coinPosition).sqrMagnitude;
                    var selfDistance = (self.transform.position - coinPosition).sqrMagnitude;
                    component.SetTarget(selfDistance < targetDistance ? self : target.Value);
                }
            }
        }
    }

    private static void FetchInteractors(On.PlayerInteraction.orig_FetchInteractors original, PlayerInteraction self)
    {
        var data = self.GetComponent<PlayerNetworkData>();
        if (data == null || data.IsLocal)
        {
            original(self);
        }
    }

    private static void RunInteraction(On.PlayerInteraction.orig_RunInteraction original, PlayerInteraction self)
    {
        var data = self.GetComponent<PlayerNetworkData>();
        if (data == null || data.IsLocal)
        {
            original(self);
        }
    }
}