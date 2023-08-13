using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using ThronefallMP.Components;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Debug = System.Diagnostics.Debug;

namespace ThronefallMP.Patches;

public class TreasuryUIPatch
{
	public static bool OverrideFocus = false;
	
    public static void Apply()
    {
        On.TreasuryUI.Start += Start;
        On.TreasuryUI.Update += Update;

        //On.TreasuryUI.Update += Update;
        // if (this.coinQeue < 0 && this.addCounter <= 0f)
        // should be if (this.coinQeue < 0 && this.removalCounter <= 0f)
        // but doesn't matter because the removal interval is 0
    }
    
    private static void Start(On.TreasuryUI.orig_Start original, TreasuryUI self)
    {
	    Plugin.Log.LogInfo($"Registering Treasury UI");
        DayNightCycle.Instance.RegisterDaytimeSensitiveObject(self);
        var currentState = Traverse.Create(self).Field<TreasuryUI.AnimationState>("currentState");

        Traverse.Create(self).Field<Transform>("scaleTarget").Value = UIFrameManager.instance.TreasureChest.scaleTarget;
        Traverse.Create(self).Field<TextMeshProUGUI>("displayText").Value = UIFrameManager.instance.TreasureChest.balanceNumber;
        Traverse.Create(self).Method("SetState", currentState.Value).GetValue();
    }

    private static void Update(On.TreasuryUI.orig_Update original, TreasuryUI self)
    {
	    var coinQueue = Traverse.Create(self).Field<int>("coinQeue");
	    var overrideActivation = Traverse.Create(self).Field<bool>("overrideActivation");
	    var activationCounter = Traverse.Create(self).Field<float>("activationCounter");
	    var activationLifetime = Traverse.Create(self).Field<float>("activationLifetime");
	    var displayText = Traverse.Create(self).Field<TextMeshProUGUI>("displayText");
	    var instantiatedCoins = Traverse.Create(self).Field<List<GameObject>>("instantiatedCoins");
	    displayText.Value.text = $"<sprite name=\"coin\">{GlobalData.Balance}";
	    coinQueue.Value = Math.Max(GlobalData.Balance, 0) - instantiatedCoins.Value.Count;
	    if (!OverrideFocus && overrideActivation.Value)
	    {
		    activationCounter.Value = activationLifetime.Value;
	    }

	    overrideActivation.Value = OverrideFocus;
	    original(self);
    }
}