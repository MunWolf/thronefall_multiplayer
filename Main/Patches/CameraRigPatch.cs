using HarmonyLib;
using UnityEngine;

namespace ThronefallMP;

public static class CameraRigPatch
{
    public static void Apply()
    {
        On.CameraRig.Update += Update;
    }

    static void Update(On.CameraRig.orig_Update original, CameraRig self)
    {
        var cameraTarget = Traverse.Create(self).Field<Transform>("cameraTarget");
        var localData = Plugin.Instance.Network.GetPlayerData(Plugin.Instance.Network.LocalPlayer);
        if (localData != null)
        {
            cameraTarget.Value = localData.transform;
            original(self);
        }
    }
}