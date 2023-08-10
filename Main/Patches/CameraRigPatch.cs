using System.Collections;
using HarmonyLib;
using UnityEngine;

namespace ThronefallMP.Patches;

public static class CameraRigPatch
{
    public static void Apply()
    {
        On.CameraRig.Update += Update;
        On.CameraRig.TransitionToTarget += TransitionToTarget;
    }

    private static void Update(On.CameraRig.orig_Update original, CameraRig self)
    {
        var cameraTarget = Traverse.Create(self).Field<Transform>("cameraTarget");
        var localData = Plugin.Instance.PlayerManager.LocalPlayer?.Data;
        if (localData != null)
        {
            cameraTarget.Value = localData.transform;
            original(self);
        }
    }

    private static IEnumerator TransitionToTarget(On.CameraRig.orig_TransitionToTarget original, CameraRig self, Transform newTarget)
    {
        var transitionRunning = Traverse.Create(self).Field<bool>("transitionRunning");
        var targetPosition = Traverse.Create(self).Field<Vector3>("targetPosition");
        var transitionSpeed = Traverse.Create(self).Field<float>("transitionSpeed");
        var currentTarget = Traverse.Create(self).Field<Transform>("currentTarget");
        
        transitionRunning.Value = true;
        var startPosition = self.transform.position;
        var startRotation = self.transform.rotation;
        var transitionTime = 0f;
        while (newTarget != null && (targetPosition.Value != newTarget.position || self.transform.rotation != newTarget.rotation))
        {
            transitionTime = Mathf.Clamp(transitionTime, 0f, 1f);
            var num = 3f * Mathf.Pow(transitionTime, 2f) - 2f * Mathf.Pow(transitionTime, 3f);
            self.transform.position = Vector3.Lerp(startPosition, newTarget.position, num);
            self.transform.rotation = Quaternion.Lerp(startRotation, newTarget.rotation, num);
            transitionTime += Time.deltaTime * transitionSpeed.Value;
            yield return null;
        }
        
        currentTarget.Value = newTarget;
        transitionRunning.Value = false;
    }
}