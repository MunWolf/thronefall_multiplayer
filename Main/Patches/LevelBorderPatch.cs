using System.Collections;
using HarmonyLib;
using UnityEngine;

namespace ThronefallMP.Patches;

public class LevelBorderPatch
{
    public static void Apply()
    {
        On.LevelBorder.Start += Start;
        On.LevelBorder.Update += Update;
    }

    private static void Start(On.LevelBorder.orig_Start original, LevelBorder self)
    {
        var defaultColor = Traverse.Create(self).Field<Color>("defaultColor");
        var fadeOutColor = Traverse.Create(self).Field<Color>("fadeOutColor");
        defaultColor.Value = self.line.Color;
        fadeOutColor.Value = defaultColor.Value with { a = 0f };
        self.line.Color = fadeOutColor.Value;
    }

    private static void Update(On.LevelBorder.orig_Update original, LevelBorder self)
    {
        var data = Plugin.Instance.PlayerManager.LocalPlayer?.Data;
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
        if (!(timer.Value >= tickTime.Value))
        {
            return;
        }
        
        timer.Value = 0f;
        var num = Vector3.Distance(position, boxCol.Value.ClosestPoint(position));
        switch (fadedIn.Value)
        {
            case true when num > fadeInDistance.Value:
            {
                if (currentFade.Value == null)
                {
                    var iterator = fadeOut.GetValue<IEnumerator>();
                    currentFade.Value = self.StartCoroutine(iterator);
                }
                fadedIn.Value = false;
                return;
            }
            case false when num <= fadeInDistance.Value:
            {
                if (currentFade.Value == null)
                {
                    var iterator = fadeIn.GetValue<IEnumerator>();
                    currentFade.Value = self.StartCoroutine(iterator);
                }
                fadedIn.Value = true;
                break;
            }
        }
    }
}