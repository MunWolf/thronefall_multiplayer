using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using Steamworks;
using ThronefallMP.Network.Sync;
using ThronefallMP.Patches;
using ThronefallMP.UI;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.SceneManagement;

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
        public TextureRepository TextureRepository { get; private set; }

        private CheatHandler CheatHandler;
        
        // Network syncs
        [UsedImplicitly] public PingPongSync PingPongSync = new();
        [UsedImplicitly] public LevelDataSync LevelDataSync = new();
        [UsedImplicitly] public ResourceSync ResourceSync = new();
        [UsedImplicitly] public InputSync InputSync = new();
        [UsedImplicitly] public PositionSync PositionSync = new();
        [UsedImplicitly] public HpSync HpSync = new();
        [UsedImplicitly] public AllyPathfinderSync AllyPathfinderSync = new();
        [UsedImplicitly] public EnemyPathfinderSync EnemyPathfinderSync = new();
        
        private void Awake()
        {
            var enableNetworkSimulation = Config.Bind(
                "Network", "EnableSimulation", false, "Enable simulation of a bad network for debugging");
            
            Instance = this;
            TextureRepository = new TextureRepository();
            Network = Instance.gameObject.AddComponent<Network.Network>();
            PlayerManager = new Network.PlayerManager();
            CheatHandler = new CheatHandler();
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            Log = Logger;
            Log.LogInfo($"Little Endian: {BitConverter.IsLittleEndian}");

            if (SteamManager.Initialized)
            {
                SetSteamNetworkConfigValues();
                if (enableNetworkSimulation.Value)
                {
                    SetSteamNetworkSimulationValues();
                }
            }
            
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
            PerkHpModifyerPatch.Apply();
            PerkWeaponModifyerPatch.Apply();
            PlayerAttackPatch.Apply();
            PlayerInteractionPatch.Apply();
            PlayerMovementPatch.Apply();
            PlayerSceptPatch.Apply();
            RevivePanelPatch.Apply();
            SceneTransitionManagerPatch.Apply();
            SteamManagerPatch.Apply();
            TreasuryUIPatch.Apply();
            UIFramePatch.Apply();
            UnitRespawnerForBuildingsPatch.Apply();
            WeaponEquipperPatch.Apply();
            
            // Apply settings.
            Application.runInBackground = true;
            SceneManager.sceneLoaded += OnSceneChanged;
        }

        private void OnUIInitialized()
        {
            UIManager.Initialize();
            var enableNetworkSimulation = Config.Bind(
                "Network", "EnableSimulation", false, "Enable simulation of a bad network for debugging");
            
            if (enableNetworkSimulation.Value)
            {
                UIManager.CreateMessageDialog("Network Debug Simulation", "Simulation of a bad network is enabled");
            }
        }
        
        public delegate void LoadCallback();

        private static Dictionary<string, List<(bool waitForTransition, LoadCallback callback)>> _loadCallbacks = new(); 
        private void OnSceneChanged(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "_UI")
            {
                OnUIInitialized();
            }

            if (_loadCallbacks.TryGetValue(scene.name, out var callbacks))
            {
                foreach (var data in callbacks)
                {
                    if (data.waitForTransition)
                    {
                        CallbackOnFinishTransition(data.callback);
                    }
                    else
                    {
                        data.callback();
                    }
                }
                
                callbacks.Clear();
            }
        }

        public static void CallbackOnLoad(string scene, bool waitForTransition, LoadCallback callback)
        {
            if (!_loadCallbacks.TryGetValue(scene, out var callbacks))
            {
                callbacks = new List<(bool waitForTransition, LoadCallback callback)>();
                _loadCallbacks[scene] = callbacks;
            }
            
            callbacks.Add((waitForTransition, callback));
        }

        private static void CallbackOnFinishTransition(LoadCallback callback)
        {
            Instance.StartCoroutine(WaitForTransition(callback));
        }
    
        private static IEnumerator WaitForTransition(LoadCallback callback)
        {
            while (SceneTransitionManager.instance.SceneTransitionIsRunning)
            {
                yield return null;
            }
        
            callback?.Invoke();
        }

        private void SetSteamNetworkConfigValues()
        {
            SetSteamNetworkValue(
                ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_TimeoutInitial,
                ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32,
                1600
            );
            
            SetSteamNetworkValue(
                ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_TimeoutConnected,
                ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32,
                3200
            );
        }

        private void SetSteamNetworkSimulationValues()
        {
            SetSteamNetworkValue(
                ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_FakePacketLoss_Send,
                ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Float,
                Config.Bind("Network", "PacketLossSendPercentage", 0.3f).Value
            );
            
            SetSteamNetworkValue(
                ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_FakePacketLoss_Recv,
                ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Float,
                Config.Bind("Network", "PacketLossReceivePercentage", 0.3f).Value
            );
            
            SetSteamNetworkValue(
                ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_FakePacketLag_Send,
                ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32,
                Config.Bind("Network", "PacketLagSendMs", 80).Value
            );
            
            SetSteamNetworkValue(
                ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_FakePacketLag_Recv,
                ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32,
                Config.Bind("Network", "PacketLagReceiveMs", 80).Value
            );
            
            SetSteamNetworkValue(
                ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_FakePacketReorder_Send,
                ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Float,
                Config.Bind("Network", "PacketReorderPercentageSend", 0.1f).Value
            );
            
            SetSteamNetworkValue(
                ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_FakePacketReorder_Recv,
                ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Float,
                Config.Bind("Network", "PacketReorderPercentageReceive", 0.1f).Value
            );
            
            SetSteamNetworkValue(
                ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_FakePacketReorder_Time,
                ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32,
                Config.Bind("Network", "PacketReorderTime", 20).Value
            );
            
            SetSteamNetworkValue(
                ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_FakePacketDup_Send,
                ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Float,
                Config.Bind("Network", "PacketDuplicatePercentSend", 0.1f).Value
            );
            
            SetSteamNetworkValue(
                ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_FakePacketDup_Send,
                ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Float,
                Config.Bind("Network", "PacketDuplicatePercentReceive", 0.1f).Value
            );
            
            SetSteamNetworkValue(
                ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_FakePacketDup_TimeMax,
                ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32,
                Config.Bind("Network", "PacketDuplicateTimeMax", 60).Value
            );
        }

        private static void SetSteamNetworkValue<T>(ESteamNetworkingConfigValue name, ESteamNetworkingConfigDataType type, T value) where T : unmanaged
        {
            var handle = GCHandle.Alloc(value, GCHandleType.Pinned);
            SteamNetworkingUtils.SetConfigValue(
                name,
                ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global,
                IntPtr.Zero,
                type,
                handle.AddrOfPinnedObject()
            );
            handle.Free();
        }
    }
}
