using System.Collections.Generic;
using HarmonyLib;
using ThronefallMP.Components;
using ThronefallMP.Network.Packets.Game;
using UnityEngine;

namespace ThronefallMP.Patches;

public class CostDisplayPatch
{
    public static void Apply()
    {
        On.CostDisplay.Deny += Deny;
        On.CostDisplay.CancelFill += CancelFill;
    }

    private static void Deny(On.CostDisplay.orig_Deny original, CostDisplay self)
    {
        var currentlyActiveCoinslots = Traverse.Create(self).Field<List<Coinslot>>("currentlyActiveCoinslots");
        var currentlyFilledCoins = Traverse.Create(self).Field<int>("currentlyFilledCoins");
        if (GlobalData.Balance < 0 && currentlyActiveCoinslots.Value.Count > 0)
        {
            currentlyActiveCoinslots.Value[currentlyFilledCoins.Value].SetEmpty();
        }
        
        while (GlobalData.Balance < 0 && currentlyFilledCoins.Value > 0)
        {
            // Maybe spawn coins here for flavour?
            currentlyFilledCoins.Value -= 1;
            currentlyActiveCoinslots.Value[currentlyFilledCoins.Value].SetEmpty();
            GlobalData.Balance += 1;
        }
        
        original(self);
    }
    
    private static void CancelFill(On.CostDisplay.orig_CancelFill original, CostDisplay self, PlayerInteraction player)
    {
        var count = 0;
        var activeSlots = Traverse.Create(self).Field<List<Coinslot>>("currentlyActiveCoinslots");
        foreach (var slot in activeSlots.Value)
        {
            if (slot.isFull)
            {
                // We only spawn this locally as we will get a balance packet anyway.
                Object.Instantiate(BuildSlotPatch.CoinPrefab, slot.transform.position, slot.transform.rotation)
                    .GetComponent<Coin>().SetTarget(player);
                ++count;
            }
            
            slot.SetEmpty();
        }

        // Since we can't pick up the coins just give the balance straight away.
        if (!Plugin.Instance.Network.Server)
        {
            GlobalData.Balance += count;
        }
        
        var currentlyFilledCoins = Traverse.Create(self).Field<int>("currentlyFilledCoins");
        var denied = Traverse.Create(self).Field<bool>("denied");
        ThronefallAudioManager.Oneshot(ThronefallAudioManager.AudioOneShot.CoinFillCancel);
        currentlyFilledCoins.Value = 0;
        CostDisplay.currentlyFilledCoinsFromLastActiveDisplay = 0;
        denied.Value = false;
    }
}