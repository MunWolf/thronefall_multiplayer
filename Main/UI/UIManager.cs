using System.Collections;
using I2.Loc;
using Rewired.Integration.UnityUI;
using ThronefallMP.UI.Dialogs;
using ThronefallMP.UI.Panels;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ThronefallMP.UI;

public static class UIManager
{
    public static GameObject TitleScreen { get; private set; }
    public static LobbyListPanel LobbyListPanel { get; private set; }
    public static HostPanel HostPanel { get; private set; }
    public static ChatPanel ChatPanel { get; private set; }
    public static GameStatusPanel GameStatusPanel { get; private set; }

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
    private static GameObject _canvas;
    private static GameObject _container;
    
    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _canvas = GameObject.Find("UI Canvas");
        if (_canvas.GetComponent<StandaloneInputModule>() == null)
        {
            _canvas.AddComponent<StandaloneInputModule>();
        }
        
        _container = new GameObject("Mod UI", typeof(RectTransform));
        var containerTransform = _container.GetComponent<RectTransform>();
        containerTransform.SetParent(_canvas.transform, false);
        containerTransform.anchorMin = new Vector2(0, 0);
        containerTransform.anchorMax = new Vector2(1, 1);
        containerTransform.offsetMin = new Vector2(0, 0);
        containerTransform.offsetMax = new Vector2(0, 0);
        
        TitleScreen = GameObject.Find("UI Canvas/Title Frame").gameObject;
        var play = TitleScreen.transform.Find("Menu Items/Play").GetComponent<ThronefallUIElement>();
        var settings = TitleScreen.transform.Find("Menu Items/Settings").GetComponent<ThronefallUIElement>();
        DefaultFont = settings.GetComponent<TextMeshProUGUI>().font;

        LobbyListPanel = BaseUI.Create<LobbyListPanel>(_canvas, _container);
        HostPanel = BaseUI.Create<HostPanel>(_canvas, _container);
        ChatPanel = BaseUI.Create<ChatPanel>(_canvas, _container);
        GameStatusPanel = BaseUI.Create<GameStatusPanel>(_canvas, _container);
        
        ChatPanel.Enabled = true;
        GameStatusPanel.Enabled = true;
        
        var multiplayer = Helpers.InstantiateDisabled(settings.gameObject, settings.transform.parent);
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

    public static void CloseAllPanels()
    {
        LobbyListPanel.Enabled = false;
        HostPanel.Enabled = false;
    }

    public static WeaponDialog CreateWeaponDialog()
    {
        var dialog = BaseUI.Create<WeaponDialog>(_canvas, _container);
        dialog.Enabled = true;
        return dialog;
    }

    public static PasswordDialog CreatePasswordDialog(PasswordDialog.Confirm confirm, PasswordDialog.Cancel cancel)
    {
        var dialog = BaseUI.Create<PasswordDialog>(_canvas, _container);
        dialog.OnConfirm += confirm;
        dialog.OnCancel += cancel;
        dialog.Enabled = true;
        return dialog;
    }

    public static MessageDialog CreateMessageDialog(string title, string message, string button = null, Color? color = null, MessageDialog.ClickDelegate onClick = null)
    {
        var dialog = BaseUI.Create<MessageDialog>(_canvas, _container);
        dialog.Title = title;
        dialog.Message = message;
        if (color.HasValue)
        {
            dialog.Color = color.Value;
        }

        if (button != null)
        {
            dialog.ButtonText = button;
        }

        if (onClick != null)
        {
            dialog.OnClick += onClick;
        }
        
        dialog.Enabled = true;
        return dialog;
    }
}