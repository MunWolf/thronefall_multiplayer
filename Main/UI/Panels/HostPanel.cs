using Steamworks;
using ThronefallMP.Patches;
using ThronefallMP.UI.Controls;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Debug = System.Diagnostics.Debug;

namespace ThronefallMP.UI.Panels;

public class HostPanel : BaseUI
{
    public override string Name => "Host Panel";

    private struct LobbyCreationRequest
    {
        public string Name;
        public string Password;
        public int MaxPlayers;
        public bool CheatsEnabled;
        public ELobbyType Type;
    }

    public ButtonControl Host { get; private set; }
    
    private const int LabelWidth = 140;
    private readonly CallResult<LobbyCreated_t> _onLobbyCreatedCallResult;
    private LobbyCreationRequest? _currentRequest;
    private GameObject _window;

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
        var background = UIHelper.CreateUIObject("background", PanelRoot);
        {
            var image = background.AddComponent<Image>();
            image.type = Image.Type.Sliced;
            image.color = UIManager.TransparentBackgroundColor;
            var rectTransform = background.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(1, 1);
        }
        
        _window = UIHelper.CreateUIObject("panel", background);
        {
            var image = _window.AddComponent<Image>();
            image.type = Image.Type.Sliced;
            image.color = UIManager.DarkBackgroundColor;
            UIHelper.SetLayoutGroup<VerticalLayoutGroup>(
                _window,
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
            var rectTransform = _window.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.35f, 0.3f);
            rectTransform.anchorMax = new Vector2(0.65f, 0.7f);
        }
        
        var panel = UIHelper.CreateUIObject("panel", _window);
        {
            var image = panel.AddComponent<Image>();
            image.type = Image.Type.Sliced;
            image.color = UIManager.BackgroundColor;
            UIHelper.SetLayoutGroup<VerticalLayoutGroup>(
                panel,
                false,
                false,
                true,
                true,
                5,
                20,
                80,
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
        var nameField = UIHelper.CreateInputField(panel, "name", "Name", $"{placeholder}'s Game", LabelWidth, 24);
        var passwordField = UIHelper.CreateInputField(panel, "password", "Password", "", LabelWidth, 24);
        passwordField.contentType = TMP_InputField.ContentType.Password;
        var maxPlayersField = UIHelper.CreateInputField(panel, "max_players", "Players", "8", LabelWidth, 2);
        maxPlayersField.contentType = TMP_InputField.ContentType.DecimalNumber;
        var friendsOnlyToggle = CreateToggle(panel, "friends_only", "Friends Only", false);
        var enableCheatsToggle = CreateToggle(panel, "cheats_enabled", "Enable Cheats", false);
        
        var buttons = UIHelper.CreateUIObject("buttons", panel);
        {
            UIHelper.SetLayoutGroup<HorizontalLayoutGroup>(
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
            UIHelper.SetLayoutElement(buttons, ignoreLayout: true);
            var rectTransform = buttons.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.0f, 0.1f);
            rectTransform.anchorMax = new Vector2(1.0f, 0.3f);
        }

        Host = UIHelper.CreateButton(buttons, "host", "Host");
        UIHelper.SetLayoutElement(Host.gameObject, minWidth: 100);
        Host.OnClick += () =>
        {
            Plugin.Log.LogInfo($"Creating {(friendsOnlyToggle.Toggle.isOn ? "friends only" : "public")} lobby");
            CreateLobby(new LobbyCreationRequest
            {
                Name = nameField.text,
                MaxPlayers = int.Parse(maxPlayersField.text),
                Password = passwordField.text,
                CheatsEnabled = enableCheatsToggle.Toggle.isOn,
                Type = friendsOnlyToggle.Toggle.isOn ? ELobbyType.k_ELobbyTypeFriendsOnly : ELobbyType.k_ELobbyTypePublic
            });
            ThronefallAudioManager.Oneshot(ThronefallAudioManager.AudioOneShot.ButtonApply);
        };
        
        var back = UIHelper.CreateButton(buttons, "back", "Back");
        UIHelper.SetLayoutElement(back.gameObject, minWidth: 100);
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
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(_window.GetComponent<RectTransform>());
    }

    private static ToggleControl CreateToggle(GameObject panel, string name, string label, bool value)
    {
        var group = UIHelper.CreateUIObject($"{name}Group", panel);
        UIHelper.SetLayoutGroup<HorizontalLayoutGroup>(
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
        UIHelper.SetLayoutElement(group, flexibleWidth: 1);

        var bg = UIHelper.CreateBox(group, $"{name}_label_bg", Color.clear);
        UIHelper.SetLayoutGroup<HorizontalLayoutGroup>(
            bg,
            childControlWidth: true,
            childAlignment: TextAnchor.MiddleLeft
        );
        UIHelper.SetLayoutElement(bg.gameObject, minWidth: LabelWidth, flexibleWidth: 0);
        bg.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var labelText = UIHelper.CreateText(bg, $"{name}_label", $"{label}: ");
        labelText.alignment = TextAlignmentOptions.Left;
        
        bg = UIHelper.CreateBox(group, $"{name}_bg", Color.clear);
        UIHelper.SetLayoutGroup<HorizontalLayoutGroup>(
            bg,
            childControlWidth: true,
            childAlignment: TextAnchor.MiddleCenter
        );
        UIHelper.SetLayoutElement(bg.gameObject, flexibleWidth: 1);
        bg.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var toggle = UIHelper.CreateToggle(bg, $"{name}", value);
        UIHelper.SetLayoutGroup<HorizontalLayoutGroup>(toggle.gameObject, childAlignment: TextAnchor.MiddleCenter);
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
        
        _window.SetActive(false);
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
        _window.SetActive(true);
        if (ioFailure)
        {
            UIManager.CreateMessageDialog("Error creating lobby",
                $"Lobby creation failed with io error'");
            return;
        }
        
        if (created.m_eResult != EResult.k_EResultOK)
        {
            UIManager.CreateMessageDialog("Error creating lobby",
                $"Lobby creation failed with error '{created.m_eResult}'");
            return;
        }

        Debug.Assert(_currentRequest != null, nameof(_currentRequest) + " != null");
        var id = new CSteamID(created.m_ulSteamIDLobby);
        var password = _currentRequest.Value.Password;
        var hasPassword = string.IsNullOrEmpty(password) ? "no" : "yes";
        SteamMatchmaking.SetLobbyData(id, "name", _currentRequest.Value.Name);
        SteamMatchmaking.SetLobbyData(id, "password", hasPassword);
        SteamMatchmaking.SetLobbyData(id, "version", Plugin.VersionString);
        // TODO: Add an option in the UI for this.
        SteamMatchmaking.SetLobbyData(id, "cheats_enabled", _currentRequest.Value.CheatsEnabled ? "yes" : "no");
        Plugin.Log.LogInfo($"Lobby {created.m_ulSteamIDLobby} created with name '{_currentRequest.Value.Name}' password '{hasPassword}'");
        Plugin.CallbackOnLoad("_LevelSelect", false, () =>
        {
            Plugin.Instance.Network.Host(id, _currentRequest.Value.Password);
            _currentRequest = null;
        });

        Enabled = false;
        UIManager.LobbyListPanel.Close();
        SceneTransitionManager.instance.TransitionFromNullToLevelSelect();
    }
}