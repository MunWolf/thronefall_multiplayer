using System.Collections.Generic;
using Steamworks;
using ThronefallMP.Network;
using ThronefallMP.UI.Controls;
using UnityEngine;
using UniverseLib.UI;
using Image = UnityEngine.UI.Image;

namespace ThronefallMP.UI.Panels;

public partial class LobbyListPanel : UniverseLib.UI.Panels.PanelBase
{
    private Callback<LobbyDataUpdate_t> _lobbyUpdated;
    private readonly CallResult<LobbyMatchList_t> _onLobbyMatchListCallResult;
    
    public override string Name => "Lobby List Panel";
    public override int MinWidth => 0;
    public override int MinHeight => 0;
    public override Vector2 DefaultAnchorMin => new(0.0f, 0.0f);
    public override Vector2 DefaultAnchorMax => new(1.0f, 1.0f);
    public override bool CanDragAndResize => false;
    public LobbyItem CurrentlySelectedLobby { get; private set; }
    public override Vector2 DefaultPosition => new(
        -Owner.Canvas.renderingDisplaySize.x / 2,
        Owner.Canvas.renderingDisplaySize.y / 2
    );

    private Texture2D _lockTexture;
    private GameObject _lobbyList;
    private ToggleControl _friendsOnly;
    private ToggleControl _showWithPassword;
    private ToggleControl _showFull;
    private ButtonControl _connect;
    private ButtonControl _host;
    private ButtonControl _back;
    private CustomScrollRect _scrollRect;
    private bool _muteSound;

    private List<LobbyItem> _lobbies = new();
    private Dictionary<CSteamID, LobbyItem> _idToLobbies = new();

    public LobbyListPanel(UIBase owner) : base(owner)
    {
        if (!SteamManager.Initialized)
        {
            return;
        }

        _lobbyUpdated = new Callback<LobbyDataUpdate_t>(OnLobbyDataUpdated);
        _onLobbyMatchListCallResult = CallResult<LobbyMatchList_t>.Create(OnLobbiesRefreshed);
    }
    
    private void PlaySelectSound()
    {
        if (!_muteSound)
        {
            ThronefallAudioManager.Oneshot(ThronefallAudioManager.AudioOneShot.ButtonSelect);
        }

        _muteSound = false;
    }

    private void RefreshLobbies()
    {
        ClearLobbyEntries();
        if (!SteamManager.Initialized)
        {
            return;
        }
        
        // TODO: Show loading sprite.

        if (_friendsOnly.Toggle.isOn)
        {
            var count = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
            for (var i = 0; i < count; ++i)
            {
                FriendGameInfo_t friendGameInfo;
                var friend = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
                if (SteamFriends.GetFriendGamePlayed(friend, out friendGameInfo) && friendGameInfo.m_steamIDLobby.IsValid())
                {
                    SteamMatchmaking.RequestLobbyData(friendGameInfo.m_steamIDLobby);
                }
            }            
        }
        else
        {
            //SteamMatchmaking.AddRequestLobbyListStringFilter("name", "the value", ELobbyComparison.k_ELobbyComparisonEqualToOrGreaterThan);
            SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
            if (!_showFull.Toggle.isOn)
            {
                SteamMatchmaking.AddRequestLobbyListFilterSlotsAvailable(1);
            }

            if (_showWithPassword)
            {
                SteamMatchmaking.AddRequestLobbyListStringFilter("password", "no", ELobbyComparison.k_ELobbyComparisonEqual);
            }
            
            _onLobbyMatchListCallResult.Set(SteamMatchmaking.RequestLobbyList());
        }
    }
    
    private void SelectLobby(LobbyItem item)
    {
        if (CurrentlySelectedLobby != null)
        {
            var image = CurrentlySelectedLobby.LobbyGameObject.GetComponent<Image>();
            image.color = UIManager.TransparentBackgroundColor;
        }
            
        CurrentlySelectedLobby = item;
        var image2 = CurrentlySelectedLobby.LobbyGameObject.GetComponent<Image>();
        image2.color = UIManager.SelectedTransparentBackgroundColor;

        _connect.SetInteractable(true);
        _host.Button.navigation = _host.Button.navigation with { selectOnLeft = _connect.Button };
        _back.Button.navigation = _back.Button.navigation with { selectOnRight = _connect.Button };
    }

    private void ClearLobbyEntries()
    {
        foreach (var lobby in _lobbies)
        {
            Object.Destroy(lobby.gameObject);
        }
        
        _lobbies.Clear();
        _idToLobbies.Clear();
        CurrentlySelectedLobby = null;
        
        _connect.SetInteractable(false);
        _host.NavLeft = _back.Button;
        _back.NavRight = _host.Button;
    }
    
    public void Open()
    {
        Enabled = true;
        RefreshLobbies();
        _host.Button.Select();
    }

    public void Close()
    {
        Enabled = false;
    }

    private void Back()
    {
        Close();
        UIManager.TitleScreen.SetActive(true);
        ThronefallAudioManager.Oneshot(ThronefallAudioManager.AudioOneShot.ButtonApply);
    }

    private const float UpdateCooldown = 2.0f;
    private float _timer;
    public override void Update()
    {
        if (!UIManager.HostPanel.Enabled && !UIManager.ExitHandled && Input.GetKeyDown(KeyCode.Escape))
        {
            UIManager.ExitHandled = true;
            Back();
        }
        
        _timer += Time.deltaTime;
        if (_timer < UpdateCooldown)
        {
            return;
        }

        _timer = 0.0f;
        foreach (var lobby in _lobbies)
        {
            SteamMatchmaking.RequestLobbyData(lobby.LobbyInfo.Id);
        }
    }

    private void OnLobbiesRefreshed(LobbyMatchList_t list, bool ioFailure)
    {
        if (!Enabled)
        {
            return;
        }

        if (ioFailure)
        {
            var reason = SteamUtils.GetAPICallFailureReason(_onLobbyMatchListCallResult.Handle);
            // TODO: Show error dialog.
            return;
        }
        
        for (var i = 0; i < list.m_nLobbiesMatching; ++i)
        {
            var lobby = SteamMatchmaking.GetLobbyByIndex(i);
            AddLobbyEntry(new Lobby()
            {
                Id = lobby,
                Name = SteamMatchmaking.GetLobbyData(lobby, "name"),
                PlayerCount = SteamMatchmaking.GetNumLobbyMembers(lobby),
                MaxPlayerCount = SteamMatchmaking.GetLobbyMemberLimit(lobby),
                HasPassword = SteamMatchmaking.GetLobbyData(lobby, "hasPassword") == "yes",
            });
        }
        
        _scrollRect.verticalNormalizedPosition = 1.0f;
    }

    private void OnLobbyDataUpdated(LobbyDataUpdate_t data)
    {
        if (!Enabled)
        {
            return;
        }

        if (data.m_ulSteamIDLobby != data.m_ulSteamIDMember)
        {
            return;
        }

        var id = new CSteamID(data.m_ulSteamIDLobby);
        if (!_idToLobbies.TryGetValue(id, out var lobby))
        {
            lobby = AddLobbyEntry(new Lobby()
            {
                Id = id,
                Name = SteamMatchmaking.GetLobbyData(id, "name"),
                PlayerCount = SteamMatchmaking.GetNumLobbyMembers(id),
                MaxPlayerCount = SteamMatchmaking.GetLobbyMemberLimit(id),
                HasPassword = SteamMatchmaking.GetLobbyData(id, "hasPassword") == "yes",
            });
        }
        else if (data.m_bSuccess == 0)
        {
            if (lobby == CurrentlySelectedLobby)
            {
                CurrentlySelectedLobby = null;
                _connect.SetInteractable(false);
                _host.NavLeft = _back.Button;
                _back.NavRight = _host.Button;
            }
            
            Object.Destroy(lobby.gameObject);
            _lobbies.Remove(lobby);
            _idToLobbies.Remove(lobby.LobbyInfo.Id);
        }
        else
        {
            lobby.LobbyInfo.PlayerCount = SteamMatchmaking.GetNumLobbyMembers(lobby.LobbyInfo.Id);
            lobby.LobbyInfo.MaxPlayerCount = SteamMatchmaking.GetLobbyMemberLimit(lobby.LobbyInfo.Id);
            lobby.PlayerCount.text = $"{lobby.LobbyInfo.PlayerCount}/{lobby.LobbyInfo.MaxPlayerCount}";
        }
    }
}