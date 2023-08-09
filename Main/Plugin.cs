using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using ThronefallMP.Network;
using ThronefallMP.Patches;
using ThronefallMP.Steam;
using ThronefallMP.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThronefallMP
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, "Thronefall Multiplayer", PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Thronefall.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public static readonly System.Random Random = new();
        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log { get; private set; }

        public NetworkManager Network;
        
        private void Awake()
        {
            Instance = this;
            Network = new NetworkManager();
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

            var networkManager = new GameObject("Network Manager");
            DontDestroyOnLoad(networkManager);
            networkManager.AddComponent<Matchmaking>();
            
            // Apply settings.
            Application.runInBackground = true;
            SceneManager.sceneLoaded += OnSceneChanged;
        }

        private void Update()
        {
            // if (Input.GetKeyDown(KeyCode.I))
            // {
            //     Logger.LogInfo($"Local game");
            //     Network.Local();
            // }
            // if (Input.GetKeyDown(KeyCode.O))
            // {
            //     Logger.LogInfo($"Hosting game");
            //     Network.Host(1000);
            // }
            // if (Input.GetKeyDown(KeyCode.P))
            // {
            //     Logger.LogInfo($"Connecting...");
            //     Network.Connect("127.0.0.1", 1000);
            // }
            // if (Input.GetKeyDown(KeyCode.K))
            // {
            //     Logger.LogInfo($"Reinstanciating all players");
            //     Network.ReinstanciatePlayers();
            // }
            
            Network.Update();
        }
        
        private static void OnSceneChanged(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "_UI")
            {
                UIManager.Initialize();
            }
        }
    }
}
