using ThronefallMP.UI.Controls;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib.UI;

namespace ThronefallMP.UI.Panels;

public class GameStatusPanel : BaseUI
{
    private static readonly Color BackgroundColor = new(0.11f, 0.11f, 0.11f, 0.95f);
    private static readonly Color EntryColor = new(0.2f, 0.2f, 0.2f, 1f);
    
    public override string Name => "Game Status Panel";

    private GameObject _playerList;
    private Texture2D _crownTexture;
    
    public override void ConstructPanelContent()
    {
        _crownTexture = Plugin.LoadTexture("crown.png");
        var container = UIFactory.CreateUIObject("container", PanelRoot);
        {
            var rectTransform = container.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0.5f);
            rectTransform.anchorMax = new Vector2(0.25f, 1);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(
                container,
                true,
                false,
                true,
                true,
                0,
                10,
                10,
                10,
                10,
                TextAnchor.UpperLeft
            );
        }
        
        _playerList = UIFactory.CreateUIObject("background", container);
        {
            _playerList.AddComponent<Mask>();
            var image = _playerList.AddComponent<Image>();
            image.type = Image.Type.Sliced;
            image.color = BackgroundColor;
            var rectTransform = _playerList.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(
                _playerList,
                true,
                false,
                true,
                true,
                5,
                10,
                10,
                10,
                10,
                TextAnchor.UpperLeft
            );
        }
        var playerEntry = UIFactory.CreateUIObject($"player_header", _playerList);
        UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(
            playerEntry,
            false,
            false,
            true,
            true,
            0,
            0,
            0,
            15,
            15,
            TextAnchor.UpperCenter
        );

        var playerName = UIHelper.CreateText(playerEntry, "name", "Name", UIManager.DefaultFont);
        playerName.alignment = TextAlignmentOptions.Left;
        playerName.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        UIFactory.SetLayoutElement(playerName.gameObject, flexibleWidth: 1);
        
        var ping = UIHelper.CreateText(playerEntry, "ping", "Ping", UIManager.DefaultFont);
        ping.alignment = TextAlignmentOptions.Center;
        ping.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        UIFactory.SetLayoutElement(ping.gameObject, minWidth: 40);
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(_playerList.GetComponent<RectTransform>());

        foreach (var player in Plugin.Instance.PlayerManager.GetAllPlayers())
        {
            CreatePlayerLine(player);
        }

        Plugin.Instance.PlayerManager.OnPlayerAdded += CreatePlayerLine;
        LayoutRebuilder.ForceRebuildLayoutImmediate(_playerList.GetComponent<RectTransform>());
        _playerList.SetActive(false);
    }

    private void Update()
    {
        if (Plugin.Instance.Network.Online && Input.GetKeyDown(KeyCode.BackQuote))
        {
            _playerList.SetActive(!_playerList.activeSelf);
            Plugin.Log.LogInfo($"List toggled {_playerList.activeSelf}");
        }
    }
    
    public void CreatePlayerLine(Network.PlayerManager.Player player)
    {
        var playerEntry = UIHelper.CreateBox(_playerList, $"player_{player.Id}", EntryColor);
        playerEntry.SetActive(false);
        UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(
            playerEntry,
            false,
            false,
            true,
            true,
            0,
            5,
            5,
            15,
            15,
            TextAnchor.MiddleCenter
        );

        var isHost = UIFactory.CreateUIObject("is_host", playerEntry);
        var image = isHost.AddComponent<Image>();
        image.type = Image.Type.Filled;
        image.sprite = Sprite.Create(
            _crownTexture,
            new Rect(0, 0, _crownTexture.width, _crownTexture.height),
            new Vector2(0.5f, 0.5f)
        );
        UIFactory.SetLayoutElement(isHost, minWidth: 24);
        
        var playerName = UIHelper.CreateText(playerEntry, "name", "", UIManager.DefaultFont);
        playerName.alignment = TextAlignmentOptions.Left;
        playerName.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        UIFactory.SetLayoutElement(playerName.gameObject, flexibleWidth: 1);
        
        var ping = UIHelper.CreateText(playerEntry, "ping", "", UIManager.DefaultFont);
        ping.alignment = TextAlignmentOptions.Center;
        ping.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        UIFactory.SetLayoutElement(ping.gameObject, minWidth: 40);

        var info = playerEntry.AddComponent<PlayerInfoControl>();
        info.playerId = player.Id;
        info.hostIdentifier = isHost;
        info.container = playerEntry;
        info.playerName = playerName;
        info.ping = ping;
        playerEntry.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_playerList.GetComponent<RectTransform>());
    }
}