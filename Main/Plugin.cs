using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
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
            CameraRigPatch.Apply();
            PlayerMovementPatch.Apply();
            
            // Apply settings.
            Application.runInBackground = true;
        }

        private void Update()
        {
            if (!Network.Online)
            {
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
            }
            
            Network.Update();
        }
    }
}
