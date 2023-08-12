using System.Collections;
using HarmonyLib;
using I2.Loc;
using ThronefallMP.Components;

namespace ThronefallMP.Patches;

public static class PlayerSceptPatch
{
    public static void Apply()
    {
        On.PlayerScept.Start += Start;
        On.PlayerScept.Update += Update;
    }

    private static void Start(On.PlayerScept.orig_Start original, PlayerScept self)
    {
        original(self);
        // Need to do this because the reference is wrong for dynamically instantiated players.
        var transform = self.transform;
        self.loadoutParent = transform.Find("Horse_LOD1/Rig/root/body/Humanoid Model Base/Civil Loadout").gameObject;
    }
    
    private static void Update(On.PlayerScept.orig_Update original, PlayerScept self)
    {
        var data = self.transform.parent.GetComponent<PlayerNetworkData>();
        if (data == null)
        {
            return;
        }
        
        if (data.SharedData.InteractButton && !data.PlayerScepterInteractLast)
        {
            self.StopAllCoroutines();
            self.StartCoroutine(Traverse.Create(self).Method("AnimationIn").GetValue<IEnumerator>());
        }
        if (!data.SharedData.InteractButton && data.PlayerScepterInteractLast)
        {
            self.StopAllCoroutines();
            self.StartCoroutine(Traverse.Create(self).Method("AnimationOut").GetValue<IEnumerator>());
        }

        data.PlayerScepterInteractLast = data.SharedData.InteractButton;
    }
}