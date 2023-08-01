using System.Collections.Generic;
using HarmonyLib;

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
        LevelData levelDataForActiveScene = LevelProgressManager.instance.GetLevelDataForActiveScene();
        int num = PlayerInteraction.instance.Networth;
        num += TagManager.instance.freeCoins.Count;
        num += self.CoinCountToBeHarvested;
        levelDataForActiveScene.dayToDayNetworth.Add(num);
        // TODO: Make coins go to the closest player instead of this.
        var component = Plugin.Instance.Network.GetPlayerData(-1).GetComponent<PlayerInteraction>();
        foreach (Coin coin in TagManager.instance.freeCoins)
        {
            if (coin.IsFree)
            {
                coin.SetTarget(component);
            }
        }
    }

    private static void DawnCallBeforeSunrise(On.DayNightCycle.orig_DawnCallBeforeSunrise original, DayNightCycle self)
    {
        var afterSunrise = Traverse.Create(self).Field<bool>("afterSunrise");
        var daytimeSensitiveObjects = Traverse.Create(self).Field<List<DayNightCycle.IDaytimeSensitive>>("daytimeSensitiveObjects");

        afterSunrise.Value = false;
        for (int i = daytimeSensitiveObjects.Value.Count - 1; i >= 0; i--)
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