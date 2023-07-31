using System.Collections;
using HarmonyLib;
using JetBrains.Annotations;
using Rewired;

namespace ThronefallMP;

public static class PlayerSceptPatch
{
    public static void Apply()
    {
        //On.PlayerScept.Update += Update;
    }

    static void Update(On.PlayerScept.orig_Update original, PlayerScept self)
    {
        var data = self.GetComponent<PlayerNetworkData>();
        if (data.SharedData.InteractButton && !data.PlayerSceptInteract)
        {
            self.StopAllCoroutines();
            self.StartCoroutine(Traverse.Create(self).Method("AnimationIn").GetValue<IEnumerator>());
        }
        if (!data.SharedData.InteractButton && data.PlayerSceptInteract)
        {
            self.StopAllCoroutines();
            self.StartCoroutine(Traverse.Create(self).Method("AnimationOut").GetValue<IEnumerator>());
        }

        data.PlayerSceptInteract = data.SharedData.InteractButton;
    }
}