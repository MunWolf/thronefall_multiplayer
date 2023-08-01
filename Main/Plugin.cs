using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using ThronefallMP.Patches;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Windows;
using Input = UnityEngine.Input;

namespace ThronefallMP
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, "Thronefall Multiplayer", PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Thronefall.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log { get; private set; }
        
        public NetworkManager Network;
        
        private void Awake()
        {
            Instance = this;
            Network = new NetworkManager();
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            Log = Logger;
            
            // Patch all the methods.
            BuildingInteractorPatch.Apply();
            BuildSlotPatch.Apply();
            CameraRigPatch.Apply();
            DayNightCyclePatch.Apply();
            LevelBorderPatch.Apply();
            NightCallPatch.Apply();
            PlayerInteractionPatch.Apply();
            PlayerMovementPatch.Apply();
            PlayerSceptPatch.Apply();
            SceneTransitionManagerPatch.Apply();
            TreasuryUIPatch.Apply();
            
            // Apply settings.
            Application.runInBackground = true;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                Logger.LogInfo($"Local game");
                Network.Local();
            }
            if (Input.GetKeyDown(KeyCode.O))
            {
                Logger.LogInfo($"Hosting game");
                Network.Host(1000);
            }
            if (Input.GetKeyDown(KeyCode.P))
            {
                Logger.LogInfo($"Connecting...");
                Network.Connect("127.0.0.1", 1000);
            }
            if (Input.GetKeyDown(KeyCode.K))
            {
                Logger.LogInfo($"Reinstanciating all players");
                Network.ReinstanciatePlayers();
            }
            
            Network.Update();
        }
    }
}
