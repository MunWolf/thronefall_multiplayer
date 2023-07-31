﻿using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Debug = System.Diagnostics.Debug;

namespace ThronefallMP.Patches;

public class TreasuryUIPatch
{
    public static void Apply()
    {
        On.TreasuryUI.Start += Start;
        On.TreasuryUI.Update += Update;
    }

    private delegate void CoinsDelegate(int amount);

    private delegate void ActivationDelegate();
    
    private static void Start(On.TreasuryUI.orig_Start original, TreasuryUI self)
    {
        DayNightCycle.Instance.RegisterDaytimeSensitiveObject(self);
        var currentState = Traverse.Create(self).Field<TreasuryUI.AnimationState>("currentState");

        Traverse.Create(self).Field<Transform>("scaleTarget").Value = UIFrameManager.instance.TreasureChest.scaleTarget;
        Traverse.Create(self).Field<TextMeshProUGUI>("displayText").Value = UIFrameManager.instance.TreasureChest.balanceNumber;
        
        SetPlayerTarget(self);
        Traverse.Create(self).Method("SetState", currentState.Value).GetValue();
    }

    private static void Update(On.TreasuryUI.orig_Update original, TreasuryUI self)
    {
        var targetPlayer = Traverse.Create(self).Field<PlayerInteraction>("targetPlayer");
        if (targetPlayer.Value == null)
        {
            SetPlayerTarget(self);
        }
        else
        {
            var isLocal = targetPlayer.Value.GetComponent<PlayerNetworkData>()?.IsLocal;
            if (!isLocal.HasValue || !isLocal.Value)
            {
                SetPlayerTarget(self);
            }
        }

        original(self);
    }

    private static void SetPlayerTarget(TreasuryUI self)
    {
        var targetPlayer = Traverse.Create(self).Field<PlayerInteraction>("targetPlayer");
        if (Plugin.Instance.Network.LocalPlayerData == null)
        {
            return;
        }
        
        targetPlayer.Value = Plugin.Instance.Network.LocalPlayerData.GetComponent<PlayerInteraction>();

        var addCoins = (CoinsDelegate)self.GetType()
            .GetMethod("AddCoins", BindingFlags.NonPublic | BindingFlags.Instance)?
            .CreateDelegate(typeof(CoinsDelegate), self);
        var removeCoins = (CoinsDelegate)self.GetType()
            .GetMethod("RemoveCoins", BindingFlags.NonPublic | BindingFlags.Instance)?
            .CreateDelegate(typeof(CoinsDelegate), self);
        var lockActivation = (ActivationDelegate)self.GetType()
            .GetMethod("LockActivation", BindingFlags.NonPublic | BindingFlags.Instance)?
            .CreateDelegate(typeof(ActivationDelegate), self);
        var unlockActivation = (ActivationDelegate)self.GetType()
            .GetMethod("UnlockActivation", BindingFlags.NonPublic | BindingFlags.Instance)?
            .CreateDelegate(typeof(ActivationDelegate), self);
        
        Debug.Assert(addCoins != null, nameof(addCoins) + " != null");
        Debug.Assert(removeCoins != null, nameof(removeCoins) + " != null");
        Debug.Assert(lockActivation != null, nameof(lockActivation) + " != null");
        Debug.Assert(unlockActivation != null, nameof(unlockActivation) + " != null");
        
        targetPlayer.Value.onBalanceGain.AddListener(new UnityAction<int>(addCoins));
        targetPlayer.Value.onBalanceSpend.AddListener(new UnityAction<int>(removeCoins));
        targetPlayer.Value.onFocusPaymentInteraction.AddListener(new UnityAction(lockActivation));
        targetPlayer.Value.onUnfocusPaymentInteraction.AddListener(new UnityAction(unlockActivation));
        addCoins(targetPlayer.Value.Balance);
    }
}