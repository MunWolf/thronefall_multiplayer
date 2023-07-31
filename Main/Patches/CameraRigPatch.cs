using HarmonyLib;
using UnityEngine;

namespace ThronefallMP.Patches;

public static class CameraRigPatch
{
    public static void Apply()
    {
        On.CameraRig.Update += Update;
    }

    private static void Update(On.CameraRig.orig_Update original, CameraRig self)
    {
        var cameraTarget = Traverse.Create(self).Field<Transform>("cameraTarget");
        var localData = Plugin.Instance.Network.LocalPlayerData;
        if (localData != null)
        {
            cameraTarget.Value = localData.transform;
            original(self);
        }
    }
}