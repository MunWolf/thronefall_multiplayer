﻿using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace ThronefallMP.Patches;

public static class DayNightCyclePatch
{
    public static void Apply()
    {
        On.DayNightCycle.DawnCallAfterSunrise += DawnCallAfterSunrise;
        On.DayNightCycle.DawnCallBeforeSunrise += DawnCallBeforeSunrise;
        On.DayNightCycle.DuskCall += DuskCall;
    }
    
    private static void DawnCallAfterSunrise(On.DayNightCycle.orig_DawnCallAfterSunrise original, DayNightCycle self)
    {
        var afterSunrise = Traverse.Create(self).Field<bool>("afterSunrise");
        var daytimeSensitiveObjects = Traverse.Create(self).Field<List<DayNightCycle.IDaytimeSensitive>>("daytimeSensitiveObjects");

        afterSunrise.Value = true;
        Hp.ReviveAllKnockedOutPlayerUnitsAndBuildings();
        for (int i = daytimeSensitiveObjects.Value.Count - 1; i >= 0; i--)
        {
            if (Utils.UnityNullCheck(daytimeSensitiveObjects.Value[i]))
            {
                daytimeSensitiveObjects.Value[i].OnDawn_AfterSunrise();
            }
            else
            {
                daytimeSensitiveObjects.Value.RemoveAt(i);
            }
        }
        ThronefallAudioManager.Oneshot(ThronefallAudioManager.AudioOneShot.BuildingRepair);
        var levelDataForActiveScene = LevelProgressManager.instance.GetLevelDataForActiveScene();
        var num = GlobalData.Networth;
        num += TagManager.instance.freeCoins.Count;
        num += self.CoinCountToBeHarvested;
        levelDataForActiveScene.dayToDayNetworth.Add(num);
        var players = Plugin.Instance.Network.GetAllPlayerData().Select(x => x.GetComponent<PlayerInteraction>()).ToArray();
        foreach (var coin in TagManager.instance.freeCoins)
        {
            var closest = Utils.FindClosest(players, coin.transform.position);
            if (coin.IsFree)
            {
                coin.SetTarget(closest);
            }
        }
    }

    private static void DawnCallBeforeSunrise(On.DayNightCycle.orig_DawnCallBeforeSunrise original, DayNightCycle self)
    {
        var afterSunrise = Traverse.Create(self).Field<bool>("afterSunrise");
        var daytimeSensitiveObjects = Traverse.Create(self).Field<List<DayNightCycle.IDaytimeSensitive>>("daytimeSensitiveObjects");

        afterSunrise.Value = false;
        for (var i = daytimeSensitiveObjects.Value.Count - 1; i >= 0; i--)
        {
            if (Utils.UnityNullCheck(daytimeSensitiveObjects.Value[i]))
            {
                daytimeSensitiveObjects.Value[i].OnDawn_BeforeSunrise();
            }
            else
            {
                daytimeSensitiveObjects.Value.RemoveAt(i);
            }
        }
        ThronefallAudioManager.Oneshot(ThronefallAudioManager.AudioOneShot.NightSurvived);
    }
    
    private static void DuskCall(On.DayNightCycle.orig_DuskCall original, DayNightCycle self)
    {
        var afterSunrise = Traverse.Create(self).Field<bool>("afterSunrise");
        var currentNightLength = Traverse.Create(self).Field<float>("currentNightLength");
        var daytimeSensitiveObjects = Traverse.Create(self).Field<List<DayNightCycle.IDaytimeSensitive>>("daytimeSensitiveObjects");

        afterSunrise.Value = false;
        currentNightLength.Value = 0f;
        for (int i = daytimeSensitiveObjects.Value.Count - 1; i >= 0; i--)
        {
            if (Utils.UnityNullCheck(daytimeSensitiveObjects.Value[i]))
            {
                daytimeSensitiveObjects.Value[i].OnDusk();
            }
            else
            {
                daytimeSensitiveObjects.Value.RemoveAt(i);
            }
        }
        
        original(self);
    }
}