using System.IO;
using System.Linq;
using System.Reflection;
using ThronefallMP.Network;
using ThronefallMP.UI.Controls;
using TMPro;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;
using UniverseLib.UI;
using Debug = System.Diagnostics.Debug;

namespace ThronefallMP.UI.Panels;

public partial class LobbyListUI
{
    public override void ConstructPanelContent()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resource = assembly.GetManifestResourceNames()
            .Single(str => str.EndsWith("lock-icon.png"));
        var stream = assembly.GetManifestResourceStream(resource);
        _lockTexture = new Texture2D(2, 2, GraphicsFormat.R8G8B8A8_UNorm, 1, TextureCreationFlags.None);
        using(var memoryStream = new MemoryStream())
        {
            Debug.Assert(stream != null, nameof(stream) + " != null");
            stream.CopyTo(memoryStream);
            _lockTexture.LoadImage(memoryStream.ToArray());
        }
        
        // TODO: Fix select sound and click sound playing when you click a button with the mouse.
        
        var multiplayer = UIFactory.CreateUIObject("multiplayer", PanelRoot);
        UIFactory.SetLayoutGroup<VerticalLayoutGroup>(
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
        
        var lobbiesUI = UIFactory.CreateUIObject("lobbies", multiplayer);
        UIFactory.SetLayoutGroup<VerticalLayoutGroup>(
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
        UIFactory.SetLayoutElement(lobbiesUI, flexibleHeight: 1);

        var header = UIFactory.CreateUIObject("header", lobbiesUI);
        UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(
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
        UIFactory.SetLayoutElement(header, minHeight: 40, flexibleHeight: 0);

        var text = UIHelper.CreateText(header, "name", "Name");
        text.alignment = TextAlignmentOptions.BaselineLeft;
        UIFactory.SetLayoutElement(text.gameObject, flexibleWidth: 9999999);
        
        text = UIHelper.CreateText(header, "player_count", "Player Count");
        text.alignment = TextAlignmentOptions.Baseline;
        UIFactory.SetLayoutElement(text.gameObject, minWidth: 160);
        
        text = UIHelper.CreateText(header, "password", "");
        text.alignment = TextAlignmentOptions.Baseline;
        UIFactory.SetLayoutElement(text.gameObject, minWidth: 50);
        
        var separator = UIFactory.CreateUIObject("separator", lobbiesUI);
        var image = separator.AddComponent<Image>();
        image.type = Image.Type.Sliced;
        image.color = UIManager.TextColor;
        UIFactory.SetLayoutElement(separator, minHeight: 2, flexibleHeight: 0);
        
        var scroller = UIFactory.CreateUIObject("scroller", lobbiesUI);
        scroller.gameObject.AddComponent<RectMask2D>();
        
        _lobbyList = UIFactory.CreateUIObject("lobby_list", scroller);
        UIFactory.SetLayoutGroup<VerticalLayoutGroup>(
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
        UIFactory.SetLayoutElement(scroller, flexibleHeight: 1);
        
        separator = UIFactory.CreateUIObject("separator", lobbiesUI);
        image = separator.AddComponent<Image>();
        image.type = Image.Type.Sliced;
        image.color = UIManager.TextColor;
        UIFactory.SetLayoutElement(separator, minHeight: 2, flexibleHeight: 0);
        
        var filters = UIFactory.CreateUIObject("filters", multiplayer);
        UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(
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
        UIFactory.SetLayoutElement(filters, minHeight: 40, flexibleHeight: 0);

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
        UIFactory.SetLayoutElement(refresh.gameObject, minWidth: 120, preferredWidth: 160);
        _friendsOnly = UIHelper.CreateLeftToggle(filters, "friends_only", "Friends Only");
        UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(_friendsOnly.gameObject, padTop: 5, padLeft: 20, padBottom: 5, padRight: 20);
        _showWithPassword = UIHelper.CreateLeftToggle(filters, "show_with_password", "Show with Password", true);
        UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(_showWithPassword.gameObject, padTop: 5, padLeft: 20, padBottom: 5, padRight: 20);
        _showFull = UIHelper.CreateLeftToggle(filters, "show_full", "Show Full", true);
        UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(_showFull.gameObject, padTop: 5, padLeft: 20, padBottom: 5, padRight: 20);
        
        var buttons = UIFactory.CreateUIObject("buttons", multiplayer);
        UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(
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
        UIFactory.SetLayoutElement(buttons, minHeight: 80, flexibleHeight: 0);

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
        UIFactory.SetLayoutElement(_connect.gameObject, minWidth: 160, preferredWidth: 300);
        _connect.SetInteractable(false);

        Host = UIHelper.CreateButton(buttons, "host", "Host");
        Host.OnClick += () =>
        {
            UIManager.HostUI.Enabled = true;
            UIManager.HostUI.Host.Button.Select();
            ThronefallAudioManager.Oneshot(ThronefallAudioManager.AudioOneShot.ButtonApply);
        };
        Host.OnExit += () => { _muteSound = false; };
        Host.OnSelected += PlaySelectSound;
        UIFactory.SetLayoutElement(Host.gameObject, minWidth: 100, preferredWidth: 300);

        _back = UIHelper.CreateButton(buttons, "back", "Back");
        _back.OnClick += Back;
        _back.OnExit += () => { _muteSound = false; };
        _back.OnSelected += PlaySelectSound;
        UIFactory.SetLayoutElement(_back.gameObject, minWidth: 100, preferredWidth: 300);

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
        ThronefallAudioManager.Oneshot(ThronefallAudioManager.AudioOneShot.ButtonApply);
    }

    private void AddLobbyEntry(Lobby info)
    {
        var lobby = UIHelper.CreateBox(_lobbyList, info.Name, UIManager.TransparentBackgroundColor);
        UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(
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
        UIFactory.SetLayoutElement(text.gameObject, flexibleWidth: 9999999);
        
        text = UIHelper.CreateText(lobby, "player_count", $"{info.PlayerCount}/{info.MaxPlayerCount}");
        text.alignment = TextAlignmentOptions.Center;
        UIFactory.SetLayoutElement(text.gameObject, minWidth: 160);
        
        var container = UIFactory.CreateUIObject("password", lobby);
        UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(
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
        UIFactory.SetLayoutElement(container, minWidth: 50);

        if (info.HasPassword)
        {
            var imageObject = UIFactory.CreateUIObject("image", container);
            var image = imageObject.gameObject.AddComponent<Image>();
            image.type = Image.Type.Filled;
            image.sprite = Sprite.Create(
                _lockTexture,
                new Rect(0, 0, _lockTexture.width, _lockTexture.height),
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