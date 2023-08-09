using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Steamworks;
using ThronefallMP.Steam;
using TMPro;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;
using UniverseLib.UI;
using Debug = System.Diagnostics.Debug;
using Image = UnityEngine.UI.Image;

namespace ThronefallMP.UI;

public class LobbyListPanel : UniverseLib.UI.Panels.PanelBase
{
    public override string Name => "Lobby List Panel";
    public override int MinWidth => 0;
    public override int MinHeight => 0;
    public override Vector2 DefaultAnchorMin => new(0.0f, 0.0f);
    public override Vector2 DefaultAnchorMax => new(1.0f, 1.0f);
    public override bool CanDragAndResize => false;
    public override Vector2 DefaultPosition => new(
        -Owner.Canvas.renderingDisplaySize.x / 2,
        Owner.Canvas.renderingDisplaySize.y / 2
    );
    
    public LobbyListPanel(UIBase owner) : base(owner) {}

    private Texture2D _lockTexture;
    private GameObject _lobbyList;
    private ButtonControl _connect;
    private ButtonControl _host;
    private ButtonControl _back;
    private CustomScrollRect _scrollRect;
    private bool _muteSound;

    private List<GameObject> _lobbies = new();
    public LobbyItem _currentlySelectedLobby;
    
    protected override void ConstructPanelContent()
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
        uiRoot.GetComponent<Image>().color = UIManager.BackgroundColor;
        ContentRoot.GetComponent<Image>().color = UIManager.BackgroundColor;
        
        var multiplayer = UIFactory.CreateUIObject("multiplayer", ContentRoot);
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
            true,
            true,
            true,
            4,
            10,
            10,
            0,
            0,
            TextAnchor.UpperCenter
        );
        var sizeFitter = _lobbyList.AddComponent<ContentSizeFitter>();
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var transform = _lobbyList.GetComponent<RectTransform>();
        transform.anchorMin = new Vector2(0, 0);
        transform.anchorMax = new Vector2(1, 1);
        
        _scrollRect = scroller.gameObject.AddComponent<CustomScrollRect>();
        _scrollRect.horizontal = false;
        _scrollRect.vertical = true;
        _scrollRect.scrollSensitivity = 50.0f;
        _scrollRect.elasticity = 0.0f;
        _scrollRect.content = _lobbyList.GetComponent<RectTransform>();
        UIFactory.SetLayoutElement(scroller, flexibleHeight: 999999);
        
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
            
        };
        _connect.OnExit += () => { _muteSound = false; };
        _connect.OnSelected += PlaySelectSound;
        UIFactory.SetLayoutElement(_connect.gameObject, minWidth: 160, preferredWidth: 300);
        _connect.SetInteractable(false);

        _host = UIHelper.CreateButton(buttons, "host", "Host");
        _host.OnClick += () =>
        {
            
        };
        _host.OnExit += () => { _muteSound = false; };
        _host.OnSelected += PlaySelectSound;
        UIFactory.SetLayoutElement(_host.gameObject, minWidth: 100, preferredWidth: 300);

        _back = UIHelper.CreateButton(buttons, "back", "Back");
        _back.OnClick += Close;
        _back.OnExit += () => { _muteSound = false; };
        _back.OnSelected += PlaySelectSound;
        UIFactory.SetLayoutElement(_back.gameObject, minWidth: 100, preferredWidth: 300);

        _connect.Button.navigation = new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnRight = _host.Button,
            selectOnLeft = _back.Button,
            wrapAround = false
        };

        _host.Button.navigation = new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnRight = _back.Button,
            selectOnLeft = _back.Button,
            wrapAround = false
        };

        _back.Button.navigation = new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnRight = _host.Button,
            selectOnLeft = _host.Button,
            wrapAround = false
        };
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(uiRoot.GetComponent<RectTransform>());
    }

    private void PlaySelectSound()
    {
        if (!_muteSound)
        {
            ThronefallAudioManager.Oneshot(ThronefallAudioManager.AudioOneShot.ButtonSelect);
        }

        _muteSound = false;
    }

    private void Close()
    {
        Enabled = false;
        UIManager.TitleScreen.SetActive(true);
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
        lobbyItem.OnClick += () => SelectLobby(lobbyItem);
        
        _lobbies.Add(lobby);
    }

    private void RefreshLobbies()
    {
        ClearLobbyEntries();
        
        AddLobbyEntry(new Lobby
        {
            Name = "Test Lobby",
            PlayerCount = 1,
            MaxPlayerCount = 4,
            HasPassword = false
        });

        AddLobbyEntry(new Lobby
        {
            Name = "The Cool Club",
            PlayerCount = 2,
            MaxPlayerCount = 8,
            HasPassword = true
        });

        for (var i = 0; i < 60; ++i)
        {
            AddLobbyEntry(new Lobby
            {
                Name = $"Sorry nobody is home {i}",
                PlayerCount = 0,
                MaxPlayerCount = 99,
                HasPassword = false
            });
        }
        
        _scrollRect.verticalNormalizedPosition = 1.0f;
    }
    
    private void SelectLobby(LobbyItem item)
    {
        if (_currentlySelectedLobby != null)
        {
            var image = _currentlySelectedLobby.LobbyGameObject.GetComponent<Image>();
            image.color = UIManager.TransparentBackgroundColor;
        }
            
        _currentlySelectedLobby = item;
        var image2 = _currentlySelectedLobby.LobbyGameObject.GetComponent<Image>();
        image2.color = UIManager.SelectedTransparentBackgroundColor;

        _connect.SetInteractable(true);
        _host.Button.navigation = _host.Button.navigation with { selectOnLeft = _connect.Button };
        _back.Button.navigation = _back.Button.navigation with { selectOnRight = _connect.Button };
    }

    private void ClearLobbyEntries()
    {
        foreach (var lobby in _lobbies)
        {
            Object.Destroy(lobby);
        }
        
        _lobbies.Clear();
        _currentlySelectedLobby = null;
    }
    
    public void Open()
    {
        Enabled = true;
        RefreshLobbies();
        _host.Button.Select();
    }
    
    public override void Update()
    {
        
    }
}