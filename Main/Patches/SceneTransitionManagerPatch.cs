using System.Collections;
using HarmonyLib;
using Steamworks;
using ThronefallMP.Components;
using ThronefallMP.Network;
using ThronefallMP.Network.Packets.Game;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThronefallMP.Patches;

public static class SceneTransitionManagerPatch
{
    public static string CurrentScene;
    public static bool InLevelSelect;
    
    private static bool _transitionHookEnabled = true;
    
    public static void Apply()
    {
        On.SceneTransitionManager.TransitionToScene += TransitionToScene;
        On.SceneTransitionManager.SceneTransitionAnimation += SceneTransitionAnimation;

        SceneManager.sceneLoaded += OnSceneChanged;
    }

    private static (string to, string from)? _queuedTransition;
    public static void Transition(string to, string from)
    {
        if (SceneTransitionManager.instance.SceneTransitionIsRunning)
        {
            _queuedTransition = (to, from);
        }
        else
        {
            var gameplayScene = Traverse.Create(SceneTransitionManager.instance).Field<string>("comingFromGameplayScene");
            gameplayScene.Value = from;
            _transitionHookEnabled = false;
            SceneTransitionManager.instance.TransitionFromNullToLevel(to);
            _transitionHookEnabled = true;
        }
    }

    private static IEnumerator SceneTransitionAnimation(On.SceneTransitionManager.orig_SceneTransitionAnimation original, SceneTransitionManager self, string scene)
    {
        var value = original(self, scene);
        if (SceneTransitionManager.instance.SceneTransitionIsRunning || !_queuedTransition.HasValue)
        {
            return value;
        }
        
        Transition(_queuedTransition.Value.to, _queuedTransition.Value.from);
        _queuedTransition = null;
        return value;
    }
    
    private static void TransitionToScene(On.SceneTransitionManager.orig_TransitionToScene original, SceneTransitionManager self, string scene)
    {
        if (_transitionHookEnabled && scene != "_StartMenu")
        {
            var packet = new RequestLevelPacket
            {
                To = scene,
                From = CurrentScene
            };
            
            foreach (var item in PerkManager.instance.CurrentlyEquipped)
            {
                packet.Perks.Add(Equip.Convert(item.name));
            }

            if (Plugin.Instance.Network.Server)
            {
                var id = new SteamNetworkingIdentity();
                id.SetSteamID(Plugin.Instance.Network.Owner);
                PacketHandler.HandlePacket(id, packet);
            }
            else
            {
                Plugin.Instance.Network.Send(packet);
            }
            UIFrameManager.instance.CloseAllFrames();
            return;
        }
        
        CurrentScene = scene;
        InLevelSelect = self.levelSelectScene == scene;
        Plugin.Log.LogInfo($"Transitioning '{scene}'");
        foreach (var data in Plugin.Instance.PlayerManager.GetAllPlayerData())
        {
            if (data == null)
            {
                continue;
            }
                
            Object.Destroy(data.gameObject);
        }
        
        original(self, scene);
    }

    private static GameObject _networkRoot;
    private static void OnSceneChanged(Scene scene, LoadSceneMode mode)
    {
        if (InLevelSelect)
        {
            // Repurpose this into something else.
            //InitializeLevelSelectObjects();
        }
        else if (_networkRoot != null)
        {
            Object.Destroy(_networkRoot);
        }
    }

    private static void InitializeLevelSelectObjects()
    {
        _networkRoot = new GameObject("Network")
        {
            transform = { position = new Vector3(26.3f, 0.0f, 19.0f) }
        };
        var flag = Object.Instantiate(GameObject.Find("Levels/Neuland/Unlocked Level"), _networkRoot.transform);
        flag.transform.localPosition = new Vector3(25.0f - 26.3f, 0.05f, 16.4f - 19.0f);
        // TODO: Add interaction component
        
        var panel = Object.Instantiate(GameObject.Find("Levels/Neuland/Quest Counter"), _networkRoot.transform);
        panel.transform.localPosition = new Vector3(25.0f - 26.3f, 11.2f, 16.4f - 19.0f);
        Object.Destroy(panel.transform.Find("Quests Canvas/Focus Panel").gameObject);
        Object.Destroy(panel.transform.Find("Quests Canvas/Background").gameObject);
        
        var text = panel.transform.Find("Quests Canvas/Cant be Played Panel").gameObject;
        text.transform.localPosition = Vector3.zero;
        text.GetComponentInChildren<TextMeshProUGUI>().text = "Multiplayer";
        text.SetActive(false);

        var interactor = flag.AddComponent<MpFlagInteractor>();
        interactor.Indicator = text;
        interactor.InteractionDistance = 3.5f;
        
        var outline = Object.Instantiate(GameObject.Find("Terrain/LevelOutlines/Pathline"), _networkRoot.transform);
        outline.transform.localPosition = new Vector3(0.0f, 0.05f, 0.0f);
        while (outline.transform.childCount > 0)
        {
            Object.DestroyImmediate(outline.transform.GetChild(0).gameObject);
        }
        
        var outlinePoints = new []
        {
            new Vector3(14.5f - 26.3f, 0.0f, 22.8f - 19.0f),
            new Vector3(22.6f - 26.3f, 0.0f, 6.0f - 19.0f),
            new Vector3(30.7f - 26.3f, 0.0f, 10.1f - 19.0f),
            new Vector3(35.34f - 26.3f, 0.0f, 25.1f - 19.0f),
            new Vector3(28.2f - 26.3f, 0.0f, 31.2f - 19.0f)
        };
        
        foreach (var point in outlinePoints)
        {
            var obj = new GameObject();
            obj.transform.parent = outline.transform;
            obj.transform.localPosition = point;
            obj.transform.localScale = new Vector3(0.2f, 0.4f, 0.4f);
        }
        
        var path = outline.GetComponent<PathMesher>();
        path.UpdateMesh();

        var plane = Object.Instantiate(GameObject.Find("Terrain/Planes/Neuland"), _networkRoot.transform);
        plane.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
        plane.transform.localScale = new Vector3(1, 1, 1);
        plane.transform.rotation = Quaternion.Euler(0, 0, 0);
        var mesh = new Mesh
        {
            name = "Network Land",
            vertices = outlinePoints,
            normals = new []
            {
                Vector3.up,
                Vector3.up,
                Vector3.up,
                Vector3.up,
                Vector3.up
            },
            triangles = new[]
            {
                0, 2, 1,
                0, 3, 2,
                0, 4, 3
            }
        };
        
        var filter = plane.GetComponent<MeshFilter>();
        filter.mesh = mesh;
        
        var renderer = plane.GetComponent<MeshRenderer>();
        renderer.material = new Material(renderer.material)
        {
            color = new Color(0.4f, 0.3f, 0.15f)
        };
    }
}