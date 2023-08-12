using HarmonyLib;
using Rewired;
using ThronefallMP.Components;
using ThronefallMP.Network.Packets.Game;
using UnityEngine;

namespace ThronefallMP.Patches;

public static class PlayerInteractionPatch
{
    public static void Apply()
    {
        On.PlayerInteraction.FetchCoins += FetchCoins;
        On.PlayerInteraction.FetchInteractors += FetchInteractors;
        On.PlayerInteraction.RunInteraction += RunInteraction;
        On.PlayerInteraction.AddCoin += AddCoin;
        On.PlayerInteraction.SpendCoins += SpendCoins;
    }

    private static void FetchCoins(On.PlayerInteraction.orig_FetchCoins original, PlayerInteraction self)
    {
        Collider[] array = Physics.OverlapSphere(self.transform.position, self.coinMagnetRadius, self.coinLayer);
        foreach (var collider in array)
        {
            var component = collider.GetComponent<Coin>();
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
        if (data != null && !data.IsLocal)
        {
            return;
        }
        
        original(self);
        TreasuryUIPatch.OverrideFocus = self.FocussedInteractor is BuildingInteractor;
    }

    private static void RunInteraction(On.PlayerInteraction.orig_RunInteraction original, PlayerInteraction self)
    {
        var data = self.GetComponent<PlayerNetworkData>();
        if (data == null || !data.IsLocal || LocalGamestate.Instance.PlayerFrozen)
        {
            return;
        }

        var input = Traverse.Create(self).Field<Player>("input");
        var focussedInteractor = Traverse.Create(self).Field<InteractorBase>("focussedInteractor");
        if (input.Value.GetButtonDown("Interact"))
        {
            if (focussedInteractor.Value != null)
            {
                focussedInteractor.Value.InteractionBegin(self);
            }
            else if (self.EquippedWeapon != null)
            {
                var packet = new ManualAttackPacket { Player = data.id };
                Plugin.Instance.Network.Send(packet, true);
            }
        }

        if (focussedInteractor.Value != null)
        {
            if (input.Value.GetButton("Interact"))
            {
                focussedInteractor.Value.InteractionHold(self);
            }
            
            if (input.Value.GetButtonUp("Interact"))
            {
                focussedInteractor.Value.InteractionEnd(self);
            }
        }
        
        BuildingInteractor.displayAllBuildPreviews = input.Value.GetButton("Preview Build Options");
    }

    private static void AddCoin(On.PlayerInteraction.orig_AddCoin original, PlayerInteraction self, int amount)
    {
        if (!Plugin.Instance.Network.Server)
        {
            // This function is used by Coin and EnemySpawner, both times we only need to handle them on the server.
            return;
        }
        
        GlobalData.Internal.NetWorth += amount;
        GlobalData.Balance += amount;
    }

    private static void SpendCoins(On.PlayerInteraction.orig_SpendCoins orig, PlayerInteraction self, int amount)
    {
        GlobalData.Balance -= amount;
    }
}