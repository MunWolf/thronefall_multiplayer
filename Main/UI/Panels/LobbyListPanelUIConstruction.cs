using ThronefallMP.Network;
using ThronefallMP.UI.Controls;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ThronefallMP.UI.Panels;

public partial class LobbyListPanel
{
    public override void ConstructPanelContent()
    {
        // TODO: Fix select sound and click sound playing when you click a button with the mouse.
        
        var multiplayer = UIHelper.CreateUIObject("multiplayer", PanelRoot);
        UIHelper.SetLayoutGroup<VerticalLayoutGroup>(
            multiplayer,
            false,
            false,
            true,
            true,
            20,
            80,
            80,
            120,
            120,
            TextAnchor.MiddleCenter
        );
        var rectTransform = multiplayer.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(1, 1);
        
        var lobbiesUI = UIHelper.CreateUIObject("lobbies", multiplayer);
        UIHelper.SetLayoutGroup<VerticalLayoutGroup>(
            lobbiesUI,
            true,
            false,
            true,
            true,
            2,
            0,
            0,
            0,
            0,
            TextAnchor.UpperCenter
        );
        UIHelper.SetLayoutElement(lobbiesUI, flexibleHeight: 1);

        var header = UIHelper.CreateUIObject("header", lobbiesUI);
        UIHelper.SetLayoutGroup<HorizontalLayoutGroup>(
            header,
            true,
            true,
            true,
            true,
            0,
            0,
            0,
            0,
            0,
            TextAnchor.MiddleCenter
        );
        UIHelper.SetLayoutElement(header, minHeight: 40, flexibleHeight: 0);

        var text = UIHelper.CreateText(header, "name", "Name");
        text.alignment = TextAlignmentOptions.BaselineLeft;
        UIHelper.SetLayoutElement(text.gameObject, flexibleWidth: 9999999);
        
        text = UIHelper.CreateText(header, "player_count", "Player Count");
        text.alignment = TextAlignmentOptions.Baseline;
        UIHelper.SetLayoutElement(text.gameObject, minWidth: 160);
        
        text = UIHelper.CreateText(header, "password", "");
        text.alignment = TextAlignmentOptions.Baseline;
        UIHelper.SetLayoutElement(text.gameObject, minWidth: 50);
        
        var separator = UIHelper.CreateUIObject("separator", lobbiesUI);
        var image = separator.AddComponent<Image>();
        image.type = Image.Type.Sliced;
        image.color = UIManager.TextColor;
        UIHelper.SetLayoutElement(separator, minHeight: 2, flexibleHeight: 0);
        
        var scroller = UIHelper.CreateUIObject("scroller", lobbiesUI);
        scroller.gameObject.AddComponent<RectMask2D>();
        
        _lobbyList = UIHelper.CreateUIObject("lobby_list", scroller);
        UIHelper.SetLayoutGroup<VerticalLayoutGroup>(
            _lobbyList,
            true,
            false,
            true,
            true,
            4,
            10,
            10,
            0,
            0,
            TextAnchor.UpperCenter
        );
        rectTransform = _lobbyList.GetComponent<RectTransform>();
        rectTransform.pivot = new Vector2(0.5f, 1);
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(1, 1);
        var fitter = _lobbyList.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        _scrollRect = scroller.gameObject.AddComponent<CustomScrollRect>();
        _scrollRect.movementType = ScrollRect.MovementType.Clamped;
        _scrollRect.inertia = false;
        _scrollRect.horizontal = false;
        _scrollRect.vertical = true;
        _scrollRect.scrollSensitivity = 50.0f;
        _scrollRect.content = _lobbyList.GetComponent<RectTransform>();
        UIHelper.SetLayoutElement(scroller, flexibleHeight: 1);
        
        separator = UIHelper.CreateUIObject("separator", lobbiesUI);
        image = separator.AddComponent<Image>();
        image.type = Image.Type.Sliced;
        image.color = UIManager.TextColor;
        UIHelper.SetLayoutElement(separator, minHeight: 2, flexibleHeight: 0);
        
        var filters = UIHelper.CreateUIObject("filters", multiplayer);
        UIHelper.SetLayoutGroup<HorizontalLayoutGroup>(
            filters,
            false,
            true,
            true,
            true,
            0,
            0,
            0,
            20,
            20,
            TextAnchor.MiddleLeft
        );
        UIHelper.SetLayoutElement(filters, minHeight: 40, flexibleHeight: 0);

        var refresh = UIHelper.CreateButton(filters, "refresh", "Refresh");
        refresh.Normal.Size = 24;
        refresh.Hover.Size = 28;
        refresh.NormalSelected.Size = 28;
        refresh.HoverSelected.Size = 28;
        refresh.Noninteractive.Size = 24;
        refresh.Reset();
        refresh.OnClick += () =>
        {
            RefreshLobbies();
            ThronefallAudioManager.Oneshot(ThronefallAudioManager.AudioOneShot.ButtonApply);
        };
        UIHelper.SetLayoutElement(refresh.gameObject, minWidth: 120, preferredWidth: 160);
        _friendsOnly = UIHelper.CreateLeftToggle(filters, "friends_only", "Friends Only");
        UIHelper.SetLayoutGroup<HorizontalLayoutGroup>(_friendsOnly.gameObject, padTop: 5, padLeft: 20, padBottom: 5, padRight: 20);
        _showWithPassword = UIHelper.CreateLeftToggle(filters, "show_with_password", "Show with Password", true);
        UIHelper.SetLayoutGroup<HorizontalLayoutGroup>(_showWithPassword.gameObject, padTop: 5, padLeft: 20, padBottom: 5, padRight: 20);
        _showFull = UIHelper.CreateLeftToggle(filters, "show_full", "Show Full", true);
        UIHelper.SetLayoutGroup<HorizontalLayoutGroup>(_showFull.gameObject, padTop: 5, padLeft: 20, padBottom: 5, padRight: 20);
        
        var buttons = UIHelper.CreateUIObject("buttons", multiplayer);
        UIHelper.SetLayoutGroup<HorizontalLayoutGroup>(
            buttons,
            false,
            true,
            true,
            true,
            0,
            0,
            0,
            20,
            20,
            TextAnchor.MiddleCenter
        );
        UIHelper.SetLayoutElement(buttons, minHeight: 80, flexibleHeight: 0);

        _connect = UIHelper.CreateButton(buttons, "connect", "Connect");
        _connect.OnClick += () =>
        {
            if (_currentlySelectedLobby.LobbyInfo.HasPassword)
            {
                UIManager.CreatePasswordDialog(
                    ConnectToLobby,
                    () => {}
                );
            }
            else
            {
                ConnectToLobby(null);
            }
        };
        _connect.OnExit += () => { _muteSound = false; };
        _connect.OnSelected += PlaySelectSound;
        UIHelper.SetLayoutElement(_connect.gameObject, minWidth: 160, preferredWidth: 300);
        _connect.SetInteractable(false);

        Host = UIHelper.CreateButton(buttons, "host", "Host");
        Host.OnClick += () =>
        {
            UIManager.HostPanel.Enabled = true;
            UIManager.HostPanel.Host.Button.Select();
            ThronefallAudioManager.Oneshot(ThronefallAudioManager.AudioOneShot.ButtonApply);
        };
        Host.OnExit += () => { _muteSound = false; };
        Host.OnSelected += PlaySelectSound;
        UIHelper.SetLayoutElement(Host.gameObject, minWidth: 100, preferredWidth: 300);

        _back = UIHelper.CreateButton(buttons, "back", "Back");
        _back.OnClick += Back;
        _back.OnExit += () => { _muteSound = false; };
        _back.OnSelected += PlaySelectSound;
        UIHelper.SetLayoutElement(_back.gameObject, minWidth: 100, preferredWidth: 300);

        // Navigation
        _connect.NavLeft = _back.Button;
        _connect.NavRight = Host.Button;
        Host.NavLeft = _back.Button;
        Host.NavRight = _back.Button;
        _back.NavLeft = Host.Button;
        _back.NavRight = Host.Button;
    }

    private void ConnectToLobby(string password)
    {
        // TODO: Add callbacks to this function for status of request.
        Plugin.Instance.Network.ConnectLobby(
            _currentlySelectedLobby.LobbyInfo.Id,
            password
        );
        _activeConnectionDialog = UIManager.CreateMessageDialog(
            "Connecting",
            $"Connecting to {_currentlySelectedLobby.LobbyInfo.Name} please wait...",
            "Cancel",
            onClick: () =>
            {
                _activeConnectionDialog = null;
                Plugin.Instance.Network.Local();
            }
        );
        ThronefallAudioManager.Oneshot(ThronefallAudioManager.AudioOneShot.ButtonApply);
    }

    public void ShowHideConnectingDialog(bool show)
    {
        if (_activeConnectionDialog == null)
        {
            return;
        }
        
        _activeConnectionDialog.gameObject.SetActive(show);
    }

    public void CloseConnectingDialog()
    {
        if (_activeConnectionDialog == null)
        {
            return;
        }
        
        Destroy(_activeConnectionDialog.gameObject);
        _activeConnectionDialog = null;
    }

    private void AddLobbyEntry(Lobby info)
    {
        var lobby = UIHelper.CreateBox(_lobbyList, info.Name, UIManager.TransparentBackgroundColor);
        UIHelper.SetLayoutGroup<HorizontalLayoutGroup>(
            lobby,
            false,
            true,
            true,
            true,
            0,
            0,
            0,
            0,
            0,
            TextAnchor.MiddleCenter
        );
        var fitter = lobby.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        var text = UIHelper.CreateText(lobby, "name", info.Name);
        text.alignment = TextAlignmentOptions.Left;
        UIHelper.SetLayoutElement(text.gameObject, flexibleWidth: 9999999);
        
        text = UIHelper.CreateText(lobby, "player_count", $"{info.PlayerCount}/{info.MaxPlayerCount}");
        text.alignment = TextAlignmentOptions.Center;
        UIHelper.SetLayoutElement(text.gameObject, minWidth: 160);
        
        var container = UIHelper.CreateUIObject("password", lobby);
        UIHelper.SetLayoutGroup<HorizontalLayoutGroup>(
            container,
            false,
            true,
            true,
            true,
            0,
            2,
            2,
            4,
            4,
            TextAnchor.MiddleCenter
        );
        UIHelper.SetLayoutElement(container, minWidth: 50);

        if (info.HasPassword)
        {
            var imageObject = UIHelper.CreateUIObject("image", container);
            var image = imageObject.gameObject.AddComponent<Image>();
            image.type = Image.Type.Filled;
            var lockTexture = Plugin.Instance.TextureRepository.Lock;
            image.sprite = Sprite.Create(
                lockTexture,
                new Rect(0, 0, lockTexture.width, lockTexture.height),
                new Vector2(0.5f, 0.5f)
            );
        }
        
        var lobbyItem = lobby.AddComponent<LobbyItem>();
        lobbyItem.LobbyGameObject = lobby;
        lobbyItem.LobbyInfo = info;
        lobbyItem.PlayerCount = text;
        lobbyItem.OnClick += () => SelectLobby(lobbyItem);

        _lobbies.Add(lobbyItem);
        if (!_idToLobbies.ContainsKey(info.Id))
        {
            _idToLobbies.Add(info.Id, lobbyItem);
        }
    }
}