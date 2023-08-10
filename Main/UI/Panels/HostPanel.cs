using Steamworks;
using ThronefallMP.Patches;
using ThronefallMP.UI.Controls;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib.UI;
using Debug = System.Diagnostics.Debug;

namespace ThronefallMP.UI.Panels;

public class HostPanel : BasePanel
{
    public override string Name => "Host Panel";

    private struct LobbyCreationRequest
    {
        public string Name;
        public string Password;
        public int MaxPlayers;
        public ELobbyType Type;
    }

    public ButtonControl Host { get; private set; }
    
    private const int LabelSize = 140;
    private readonly CallResult<LobbyCreated_t> _onLobbyCreatedCallResult;
    private LobbyCreationRequest? _currentRequest;

    public HostPanel()
    {
        if (!SteamManager.Initialized)
        {
            return;
        }

        _onLobbyCreatedCallResult = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
    }
    
    public override void ConstructPanelContent()
    {
        var background = UIFactory.CreateUIObject("background", PanelRoot);
        {
            var image = background.AddComponent<Image>();
            image.type = Image.Type.Sliced;
            image.color = UIManager.TransparentBackgroundColor;
            var rectTransform = background.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(1, 1);
        }
        
        var panelBorders = UIFactory.CreateUIObject("panel", background);
        {
            var image = panelBorders.AddComponent<Image>();
            image.type = Image.Type.Sliced;
            image.color = UIManager.DarkBackgroundColor;
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(
                panelBorders,
                true,
                true,
                true,
                true,
                0,
                5,
                5,
                5,
                5,
                TextAnchor.MiddleLeft
            );
            var rectTransform = panelBorders.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.35f, 0.3f);
            rectTransform.anchorMax = new Vector2(0.65f, 0.7f);
        }
        
        var panel = UIFactory.CreateUIObject("panel", panelBorders);
        {
            var image = panel.AddComponent<Image>();
            image.type = Image.Type.Sliced;
            image.color = UIManager.BackgroundColor;
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(
                panel,
                false,
                false,
                true,
                true,
                5,
                20,
                20,
                60,
                60,
                TextAnchor.MiddleLeft
            );
            var rectTransform = panel.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            rectTransform.anchorMax = new Vector2(1.0f, 1.0f);
        }
        
        // TODO: Add validation on the fields, don't allow hosting unless options are valid.
        var placeholder = SteamManager.Initialized ? SteamFriends.GetPersonaName() : "Unavailable";
        var nameField = CreateField(panel, "name", "Name", $"{placeholder}'s Game", 24);
        var passwordField = CreateField(panel, "password", "Password", "", 24, TMP_InputField.ContentType.Password);
        var maxPlayersField = CreateField(panel, "max_players", "Players", "8", 2, TMP_InputField.ContentType.DecimalNumber);
        var friendsOnlyToggle = CreateToggle(panel, "friends_only", "Friends Only", false);
        
        var buttons = UIFactory.CreateUIObject("buttons", panel);
        {
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(
                buttons,
                false,
                false,
                true,
                true,
                20,
                0,
                0,
                0,
                0,
                TextAnchor.MiddleCenter
            );
            UIFactory.SetLayoutElement(buttons, ignoreLayout: true);
            var rectTransform = buttons.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.0f, 0.1f);
            rectTransform.anchorMax = new Vector2(1.0f, 0.3f);
        }

        Host = UIHelper.CreateButton(buttons, "host", "Host");
        UIFactory.SetLayoutElement(Host.gameObject, minWidth: 100);
        Host.OnClick += () =>
        {
            Plugin.Log.LogInfo($"Creating {(friendsOnlyToggle.Toggle.isOn ? "friends only" : "public")} lobby");
            var success = CreateLobby(new LobbyCreationRequest
            {
                Name = nameField.text,
                MaxPlayers = int.Parse(maxPlayersField.text),
                Password = passwordField.text,
                Type = friendsOnlyToggle.Toggle.isOn ? ELobbyType.k_ELobbyTypeFriendsOnly : ELobbyType.k_ELobbyTypePublic
            });

            if (success)
            {
                Enabled = false;
                UIManager.LobbyListPanel.Close();
                ThronefallAudioManager.Oneshot(ThronefallAudioManager.AudioOneShot.ButtonApply);
            }
        };
        
        var back = UIHelper.CreateButton(buttons, "back", "Back");
        UIFactory.SetLayoutElement(back.gameObject, minWidth: 100);
        back.OnClick += () =>
        {
            Enabled = false;
            UIManager.LobbyListPanel.Host.Button.Select();
            ThronefallAudioManager.Oneshot(ThronefallAudioManager.AudioOneShot.ButtonApply);
        };

        nameField.navigation = nameField.navigation with { selectOnDown = passwordField, selectOnUp = Host.Button };
        passwordField.navigation = passwordField.navigation with { selectOnDown = maxPlayersField, selectOnUp = nameField };
        maxPlayersField.navigation = maxPlayersField.navigation with { selectOnDown = friendsOnlyToggle.Toggle, selectOnUp = passwordField };
        friendsOnlyToggle.NavUp = maxPlayersField;
        friendsOnlyToggle.NavDown = Host.Button;
        Host.NavUp = friendsOnlyToggle.Toggle;
        Host.NavDown = nameField;
        Host.NavLeft = back.Button;
        Host.NavRight = back.Button;
        back.NavUp = friendsOnlyToggle.Toggle;
        back.NavDown = nameField;
        back.NavLeft = Host.Button;
        back.NavRight = Host.Button;
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelBorders.GetComponent<RectTransform>());
    }

    private static TMP_InputField CreateField(GameObject panel, string name, string label, string value, int limit = 32, TMP_InputField.ContentType type = TMP_InputField.ContentType.Alphanumeric)
    {
        var group = UIFactory.CreateUIObject($"{name}Group", panel);
        UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(
            group,
            false,
            false,
            true,
            false,
            20,
            5,
            5,
            5,
            5,
            TextAnchor.MiddleLeft
        );
        UIFactory.SetLayoutElement(group, flexibleWidth: 1);

        var bg = UIHelper.CreateBox(group, $"{name}_label_bg", Color.clear);
        UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(
            bg,
            childControlWidth: true,
            childAlignment: TextAnchor.MiddleLeft
        );
        UIFactory.SetLayoutElement(bg.gameObject, minWidth: LabelSize, flexibleWidth: 0);
        bg.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var labelText = UIHelper.CreateText(bg, $"{name}_label", $"{label}: ");
        labelText.alignment = TextAlignmentOptions.Left;
        
        bg = UIHelper.CreateBox(group, $"{name}_bg",  new Color(0.2f, 0.2f, 0.2f));
        UIFactory.SetLayoutElement(bg, flexibleWidth: 1);
        bg.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var inputField = bg.AddComponent<TMP_InputField>();
        inputField.textViewport = inputField.transform.parent.GetComponent<RectTransform>();
        inputField.targetGraphic = bg.GetComponent<Image>();
        
        var textArea = new GameObject("area", typeof(RectTransform));
        inputField.textViewport = textArea.GetComponent<RectTransform>();
        textArea.transform.SetParent(bg.transform);
        inputField.textViewport.localPosition = Vector3.zero;
        inputField.textViewport.anchorMin = new Vector2(0, 0);
        inputField.textViewport.anchorMax = new Vector2(1, 1);
        textArea.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        var text = UIHelper.CreateText(textArea, $"{name}", $"{value}");
        text.color = UIManager.TextColor;
        text.fontSize = 20;
        inputField.textComponent = text;
        inputField.text = text.text;
        inputField.contentType = type;
        inputField.characterLimit = limit;
        var transform = text.GetComponent<RectTransform>();
        transform.localPosition = Vector3.zero;
        transform.anchorMin = new Vector2(0, 0);
        transform.anchorMax = new Vector2(1, 1);
        inputField.onFocusSelectAll = false;

        return inputField;
    }

    private static ToggleControl CreateToggle(GameObject panel, string name, string label, bool value)
    {
        var group = UIFactory.CreateUIObject($"{name}Group", panel);
        UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(
            group,
            false,
            false,
            true,
            false,
            20,
            5,
            5,
            5,
            5,
            TextAnchor.MiddleLeft
        );
        UIFactory.SetLayoutElement(group, flexibleWidth: 1);

        var bg = UIHelper.CreateBox(group, $"{name}_label_bg", Color.clear);
        UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(
            bg,
            childControlWidth: true,
            childAlignment: TextAnchor.MiddleLeft
        );
        UIFactory.SetLayoutElement(bg.gameObject, minWidth: LabelSize, flexibleWidth: 0);
        bg.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var labelText = UIHelper.CreateText(bg, $"{name}_label", $"{label}: ");
        labelText.alignment = TextAlignmentOptions.Left;
        
        bg = UIHelper.CreateBox(group, $"{name}_bg", Color.clear);
        UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(
            bg,
            childControlWidth: true,
            childAlignment: TextAnchor.MiddleCenter
        );
        UIFactory.SetLayoutElement(bg.gameObject, flexibleWidth: 1);
        bg.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var toggle = UIHelper.CreateToggle(bg, $"{name}", value);
        UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(toggle.gameObject, childAlignment: TextAnchor.MiddleCenter);
        return toggle;
    }

    public void Update()
    {
        if (UIManager.ExitHandled || !Input.GetKeyDown(KeyCode.Escape))
        {
            return;
        }
        
        Enabled = false;
        UIManager.ExitHandled = true;
        ThronefallAudioManager.Oneshot(ThronefallAudioManager.AudioOneShot.ButtonApply);
    }
    
    private bool CreateLobby(LobbyCreationRequest request)
    {
        if (_currentRequest.HasValue)
        {
            return false;
        }
        
        _currentRequest = request;
        if (request.Password == string.Empty)
        {
            request.Password = null;
        }
        
        _onLobbyCreatedCallResult.Set(SteamMatchmaking.CreateLobby(request.Type, request.MaxPlayers));
        return true;
    }

    private void OnLobbyCreated(LobbyCreated_t created, bool ioFailure)
    {
        if (ioFailure)
        {
            // TODO: Show error message.
            return;
        }
        
        if (created.m_eResult != EResult.k_EResultOK)
        {
            // TODO: Show error message.
            return;
        }

        Debug.Assert(_currentRequest != null, nameof(_currentRequest) + " != null");
        var id = new CSteamID(created.m_ulSteamIDLobby);
        var password = _currentRequest.Value.Password;
        var hasPassword = string.IsNullOrEmpty(password) ? "no" : "yes";
        SteamMatchmaking.SetLobbyData(id, "name", _currentRequest.Value.Name);
        SteamMatchmaking.SetLobbyData(id, "password", hasPassword);
        Plugin.Log.LogInfo($"Lobby {created.m_ulSteamIDLobby} created with name '{_currentRequest.Value.Name}' password '{hasPassword}'");
        Plugin.CallbackOnLoad("_LevelSelect", () =>
        {
            Plugin.Instance.Network.Host(id, _currentRequest.Value.Password);
            _currentRequest = null;
        });

        SceneTransitionManagerPatch.DisableTransitionHook = true;
        SceneTransitionManager.instance.TransitionFromNullToLevelSelect();
        SceneTransitionManagerPatch.DisableTransitionHook = false;
    }
}