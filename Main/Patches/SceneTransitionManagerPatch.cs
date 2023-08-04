using ThronefallMP.Components;
using ThronefallMP.NetworkPackets;
using ThronefallMP.NetworkPackets.Game;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThronefallMP.Patches;

public static class SceneTransitionManagerPatch
{
    public static bool InLevelSelect;
    public static bool DisableTransitionHook;
    
    public static void Apply()
    {
        On.SceneTransitionManager.TransitionToScene += TransitionToScene;

        SceneManager.sceneLoaded += OnSceneChanged;
    }

    private static void TransitionToScene(On.SceneTransitionManager.orig_TransitionToScene original, SceneTransitionManager self, string scene)
    {
        InLevelSelect = self.levelSelectScene == scene;
        if (!DisableTransitionHook)
        {
            var packet = new TransitionToScenePacket
            {
                ComingFromGameplayScene = self.ComingFromGameplayScene,
                Level = scene,
            };
            
            foreach (var item in PerkManager.instance.CurrentlyEquipped)
            {
                packet.Perks.Add(item.name);
            }
            
            Plugin.Instance.Network.Send(packet);
        }
        
        foreach (var data in Plugin.Instance.Network.GetAllPlayerData())
        {
            PlayerManager.UnregisterPlayer(data.GetComponent<PlayerMovement>());
            Object.Destroy(data.gameObject);
        }
        
        original(self, scene);
    }

    private static GameObject _networkRoot = null;
    private static void OnSceneChanged(Scene scene, LoadSceneMode mode)
    {
        if (InLevelSelect)
        {
            InitializeLevelSelectObjects();
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