using Steamworks;
using TMPro;
using UnityEngine;

namespace ThronefallMP.UI.Controls;

public class PlayerInfoControl : MonoBehaviour
{
    private const int GoodPing = 80;
    private const int BadPing = 300;
    
    public int playerId;
    public GameObject hostIdentifier;
    public GameObject container;
    public TMP_Text playerName;
    public TMP_Text ping;
    // TODO: Add image component that will be which weapon is used.

    private void OnEnable()
    {
        var player = Plugin.Instance.PlayerManager.Get(playerId);
        if (player == null)
        {
            Destroy(gameObject);
            return;
        }
        
        hostIdentifier.gameObject.SetActive(player.SteamID == Plugin.Instance.Network.Owner);
        playerName.text = SteamManager.Initialized ? SteamFriends.GetFriendPersonaName(player.SteamID) : "Unknown";
        OnPingUpdated(player);
        Plugin.Instance.PlayerManager.OnPlayerPingUpdated += OnPingUpdated;
        Plugin.Instance.PlayerManager.OnPlayerRemoved += OnPlayerRemoved;
        Plugin.Instance.Network.OnOwnerChanged += OnOwnerChanged;
    }

    private void OnOwnerChanged(CSteamID owner)
    {
        var player = Plugin.Instance.PlayerManager.Get(playerId);
        hostIdentifier.gameObject.SetActive(player.SteamID == owner);
    }
    
    private void OnPlayerRemoved(Network.PlayerManager.Player player)
    {
        if (player.Id == playerId)
        {
            Destroy(container);
        }
    }

    private void OnDisable()
    {
        Plugin.Instance.PlayerManager.OnPlayerPingUpdated -= OnPingUpdated;
    }

    private void OnDestroy()
    {
        Plugin.Instance.PlayerManager.OnPlayerRemoved -= OnPlayerRemoved;
    }

    private void OnPingUpdated(Network.PlayerManager.Player player)
    {
        ping.text = player.Ping.ToString();
        ping.color = Color.Lerp(
            Color.green,
            Color.red,
            Mathf.Min(Mathf.Max((int)player.Ping - GoodPing, 0f) / (BadPing - GoodPing), 1f)
        );
    }
}