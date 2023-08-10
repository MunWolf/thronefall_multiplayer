using I2.Loc;
using Rewired.Integration.UnityUI;
using ThronefallMP.UI.Panels;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ThronefallMP.UI;

public static class UIManager
{
    public static GameObject TitleScreen { get; private set; }
    public static Panels.LobbyListPanel LobbyListPanel { get; private set; }
    public static Panels.HostPanel HostPanel { get; private set; }

    public static bool ExitHandled = false;
    
    public static readonly Color DarkBackgroundColor = new(0.06f, 0.06f, 0.06f, 1.0f);
    public static readonly Color BackgroundColor = new(0.11f, 0.11f, 0.11f, 1.0f);
    public static readonly Color TransparentBackgroundColor = new(0.0f, 0.0f, 0.0f, 0.3f);
    public static readonly Color SelectedTransparentBackgroundColor = new(0.2f, 0.2f, 0.2f, 0.3f);
    public static readonly Color TextColor = new(0.78f, 0.65f, 0.46f, 1.0f);
    public static readonly Color ButtonTextColor = new(0.97f, 0.88f, 0.75f, 1.0f);
    public static readonly Color NoninteractiveButtonTextColor = new(0.3f, 0.3f, 0.4f, 1.0f);
    public static readonly Color ButtonHoverTextColor = new(0.0f, 0.41f, 0.11f);
    public static readonly Color ExitButtonColor = new(0.176f, 0.165f, 0.149f);
    public static TMP_FontAsset DefaultFont;

    private static bool _initialized;
    
    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        var canvas = GameObject.Find("UI Canvas");
        var input = canvas.AddComponent<StandaloneInputModule>();
        
        var container = new GameObject("Mod UI", typeof(RectTransform)).GetComponent<RectTransform>();
        container.SetParent(canvas.transform, false);
        container.anchorMin = new Vector2(0, 0);
        container.anchorMax = new Vector2(1, 1);
        container.offsetMin = new Vector2(0, 0);
        container.offsetMax = new Vector2(0, 0);
        
        TitleScreen = GameObject.Find("UI Canvas/Title Frame").gameObject;
        var play = TitleScreen.transform.Find("Menu Items/Play").GetComponent<ThronefallUIElement>();
        var settings = TitleScreen.transform.Find("Menu Items/Settings").GetComponent<ThronefallUIElement>();
        DefaultFont = settings.GetComponent<TextMeshProUGUI>().font;

        LobbyListPanel = BasePanel.Create<LobbyListPanel>(canvas, container.gameObject);
        HostPanel = BasePanel.Create<HostPanel>(canvas, container.gameObject);
        
        var multiplayer = Utils.InstantiateDisabled(settings.gameObject, settings.transform.parent);
        multiplayer.name = "Multiplayer";
        multiplayer.transform.SetSiblingIndex(1);
        Object.DestroyImmediate(multiplayer.GetComponent<Localize>());
        var textMesh = multiplayer.GetComponent<TextMeshProUGUI>();
        textMesh.text = "Multiplayer";
        var button = multiplayer.GetComponent<TFUITextButton>();
        button.onSelectionStateChange.m_PersistentCalls.m_Calls.Clear();
        button.onApply.m_PersistentCalls.m_Calls.Clear();
        button.onApply.AddListener(() =>
        {
            LobbyListPanel.Open();
            TitleScreen.SetActive(false);
        });
 
        button.rightNav = settings;
        if (SteamManager.Initialized)
        {
            play.rightNav = button;
            settings.leftNav = button;
        }
        else
        {
            button.ignoreMouse = true;
            button.cannotBeSelected = true;
            textMesh.color = NoninteractiveButtonTextColor;
        }
        
        multiplayer.SetActive(true);
        _initialized = true;
    }
}