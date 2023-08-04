using System.Collections.Generic;
using HarmonyLib;
using ThronefallMP.Components;
using ThronefallMP.NetworkPackets;
using ThronefallMP.NetworkPackets.Game;
using UnityEngine;

namespace ThronefallMP.Patches;

public class CostDisplayPatch
{
    public static void Apply()
    {
        On.CostDisplay.CancelFill += CancelFill;
    }
    
    private static void CancelFill(On.CostDisplay.orig_CancelFill orig, CostDisplay self, PlayerInteraction player)
    {
        var activeSlots = Traverse.Create(self).Field<List<Coinslot>>("currentlyActiveCoinslots");
        foreach (var slot in activeSlots.Value)
        {
            if (slot.isFull)
            {
                var transform = slot.transform;
                var packet = new SpawnCoinPacket
                {
                    Player = player.GetComponent<PlayerNetworkData>().id,
                    Position = transform.position,
                    Rotation = transform.rotation
                };

                Plugin.Instance.Network.Send(packet, true);
            }
            
            slot.SetEmpty();
        }
        
        var currentlyFilledCoins = Traverse.Create(self).Field<int>("currentlyFilledCoins");
        var denied = Traverse.Create(self).Field<bool>("denied");
        ThronefallAudioManager.Oneshot(ThronefallAudioManager.AudioOneShot.CoinFillCancel);
        currentlyFilledCoins.Value = 0;
        CostDisplay.currentlyFilledCoinsFromLastActiveDisplay = 0;
        denied.Value = false;
    }
}