using HarmonyLib;
using MPUIKIT;
using ThronefallMP.NetworkPackets;
using ThronefallMP.NetworkPackets.Game;
using UnityEngine;
using UnityEngine.Networking;

namespace ThronefallMP.Patches;

public static class NightCallPatch
{
    public static void Apply()
    {
        On.NightCall.UpdateFill += UpdateFill;
    }

    private static void UpdateFill(On.NightCall.orig_UpdateFill original, NightCall self)
    {
        HandleNightCallAudio(self);
        var data = Plugin.Instance.Network.LocalPlayerData;
        if (data == null)
        {
            return;
        }
        
        var active = Traverse.Create(self).Field<bool>("active");
        var currentFill = Traverse.Create(self).Field<float>("currentFill");
        var background = Traverse.Create(self).Field<MPImage>("background");
        var defaultBackgroundColor = Traverse.Create(self).Field<Color>("defaultBackgroundColor");
        if (active.Value)
        {
            var player = data.GetComponent<PlayerInteraction>();
            var nightCallTargetVolume = Traverse.Create(self).Field<float>("nightCallTargetVolume");
            
            if (SettingsManager.Instance.UseLargeInGameUI)
            {
                self.scaleParent.localScale = Vector3.one * 1.5f;
            }
            else
            {
                self.scaleParent.localScale = Vector3.one;
            }
            
            if (data.SharedData.CallNightButton && player.IsFreeToCallNight)
            {
                currentFill.Value += Time.deltaTime * (1f / self.nightCallTime);
            }
            else
            {
                currentFill.Value -= Time.deltaTime * 2f * (1f / self.nightCallTime);
            }
            if (currentFill.Value >= 1f)
            {
                var packet = new DayNightPacket
                {
                    Night = true
                };

                Plugin.Instance.Network.Send(packet, true);
            }
            if (currentFill.Value > 0f)
            {
                self.nightCallCueText.gameObject.SetActive(true);
                self.nightCallTimeText.text = (self.nightCallTime * (1f - currentFill.Value)).ToString("F1") + "s";
                self.nightCallCueText.transform.localScale = Vector3.one * self.textCueScaleCurve.Evaluate(Mathf.InverseLerp(0f, 0.15f, currentFill.Value));
            }
            else
            {
                self.nightCallCueText.gameObject.SetActive(false);
                self.nightCallCueText.transform.localScale = Vector3.one;
            }
            var color = defaultBackgroundColor.Value;
            color.a = Mathf.InverseLerp(0f, 0.4f, currentFill.Value);
            defaultBackgroundColor.Value = color;
            self.background.color = color;
            currentFill.Value = Mathf.Clamp01(currentFill.Value);
            self.targetGraphic.fillAmount = currentFill.Value;
            return;
        }
        
        if (currentFill.Value > 0f)
        {
            currentFill.Value -= Time.deltaTime * 2f;
            var color = defaultBackgroundColor.Value;
            color.a = Mathf.InverseLerp(0f, 0.4f, currentFill.Value);
            defaultBackgroundColor.Value = color;
            background.Value.color = color;
        }
    }

    private static void HandleNightCallAudio(NightCall self)
    {
        var active = Traverse.Create(self).Field<bool>("active");
        var nightCallTargetVolume = Traverse.Create(self).Field<float>("nightCallTargetVolume");
        var maxVolume = 0.0f;
        if (active.Value)
        {
            foreach (var data in Plugin.Instance.Network.GetAllPlayerData())
            {
                if (!data.CallNightLast && data.SharedData.CallNightButton)
                {
                    self.nightCallAudio.Stop();
                    self.nightCallAudio.PlayOneShot(ThronefallAudioManager.Instance.audioContent.NightCallStart, 0.45f);
                }
            
                data.CallNightLast = data.SharedData.CallNightButton;
                if (data.SharedData.CallNightFill > 0f)
                {
                    maxVolume = Mathf.Max(
                        maxVolume,
                        Mathf.Lerp(0f, nightCallTargetVolume.Value, Mathf.InverseLerp(0f, 0.3f, data.SharedData.CallNightFill))
                    );
                }
            }
        }

        self.nightCallAudio.volume = maxVolume;
    }

    public static void TriggerNightFall()
    {
        var nightCall = NightCall.instance;
        nightCall.nightCallAudio.PlayOneShot(ThronefallAudioManager.Instance.audioContent.NightCallComplete, 0.8f);
        DayNightCycle.Instance.SwitchToNight();
        nightCall.fullFeedback.PlayFeedbacks();
        var active = Traverse.Create(nightCall).Field<bool>("active");
        active.Value = false;
    }
}