using System.Collections;
using HarmonyLib;
using UnityEngine;

namespace ThronefallMP;

public class LevelBorderPatch
{
    public static void Apply()
    {
        On.LevelBorder.Update += Update;
    }

    static void Update(On.LevelBorder.orig_Update original, LevelBorder self)
    {
        var data = Plugin.Instance.Network.GetPlayerData(Plugin.Instance.Network.LocalPlayer);
        if (data == null)
        {
            return;
        }
        
        var position = data.transform.position;
        var timer = Traverse.Create(self).Field<float>("timer");
        var tickTime = Traverse.Create(self).Field<float>("tickTime");
        var boxCol = Traverse.Create(self).Field<BoxCollider>("boxCol");
        var fadedIn = Traverse.Create(self).Field<bool>("fadedIn");
        var fadeInDistance = Traverse.Create(self).Field<float>("fadeInDistance");
        var currentFade = Traverse.Create(self).Field<Coroutine>("currentFade");
        
        var fadeIn = Traverse.Create(self).Method("FadeIn");
        var fadeOut =  Traverse.Create(self).Method("FadeOut");
        
        timer.Value += Time.deltaTime;
        if (timer.Value >= tickTime.Value)
        {
            timer.Value = 0f;
            float num = Vector3.Distance(position, boxCol.Value.ClosestPoint(position));
            if (fadedIn.Value && num > fadeInDistance.Value)
            {
                if (currentFade.Value == null)
                {
                    var iterator = fadeOut.GetValue<IEnumerator>();
                    currentFade.Value = self.StartCoroutine(iterator);
                }
                fadedIn.Value = false;
                return;
            }
            if (!fadedIn.Value && num <= fadeInDistance.Value)
            {
                if (currentFade.Value == null)
                {
                    var iterator = fadeIn.GetValue<IEnumerator>();
                    currentFade.Value = self.StartCoroutine(iterator);
                }
                fadedIn.Value = true;
            }
        }
    }
}