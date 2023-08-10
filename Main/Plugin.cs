using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using ThronefallMP.Network;
using ThronefallMP.Patches;
using ThronefallMP.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using PlayerManager = ThronefallMP.Network.PlayerManager;

namespace ThronefallMP
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, "Thronefall Multiplayer", PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Thronefall.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public const string VersionString = $"thronefall_mp_{PluginInfo.PLUGIN_VERSION}";
        public static readonly System.Random Random = new();
        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log { get; private set; }
        
        public Network.Network Network { get; private set; }
        public Network.PlayerManager PlayerManager { get; private set; }
        
        private void Awake()
        {
            Instance = this;
            Network = Instance.gameObject.AddComponent<Network.Network>();
            PlayerManager = new Network.PlayerManager();
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            Log = Logger;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            
            // Patch all the methods.
            BuildingInteractorPatch.Apply();
            BuildSlotPatch.Apply();
            CameraRigPatch.Apply();
            CommandUnitsPatch.Apply();
            CostDisplayPatch.Apply();
            DayNightCyclePatch.Apply();
            EnemySpawnerPatch.Apply();
            HpPatch.Apply();
            LevelBorderPatch.Apply();
            LevelSelectManagerPatch.Apply();
            NightCallPatch.Apply();
            PathFinderMovementEnemyPatch.Apply();
            PathfindMovementPlayerunitPatch.Apply();
            PlayerAttackPatch.Apply();
            PlayerInteractionPatch.Apply();
            PlayerMovementPatch.Apply();
            PlayerSceptPatch.Apply();
            RevivePanelPatch.Apply();
            SceneTransitionManagerPatch.Apply();
            SteamManagerPatch.Apply();
            TreasuryUIPatch.Apply();
            UnitRespawnerForBuildingsPatch.Apply();
            
            // Apply settings.
            Application.runInBackground = true;
            SceneManager.sceneLoaded += OnSceneChanged;
        }
        
        public delegate void LoadCallback();

        private static Dictionary<string, List<LoadCallback>> _loadCallbacks = new(); 
        private static void OnSceneChanged(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "_UI")
            {
                UIManager.Initialize();
            }

            if (_loadCallbacks.TryGetValue(scene.name, out var callbacks))
            {
                foreach (var callback in callbacks)
                {
                    callback();
                }
                
                callbacks.Clear();
            }
        }

        public static void CallbackOnLoad(string scene, LoadCallback callback)
        {
            if (!_loadCallbacks.TryGetValue(scene, out var callbacks))
            {
                callbacks = new List<LoadCallback>();
                _loadCallbacks[scene] = callbacks;
            }
            
            callbacks.Add(callback);
        }
    }
}
