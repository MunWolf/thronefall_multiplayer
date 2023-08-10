using ThronefallMP.Network;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
// ReSharper disable InconsistentNaming

namespace ThronefallMP.UI.Controls;

public class LobbyItem : MonoBehaviour, IPointerClickHandler
{
    public delegate void HandleClick();
    public event HandleClick OnClick;

    public GameObject LobbyGameObject;
    public Lobby LobbyInfo;
    public TextMeshProUGUI PlayerCount;
    
    public void OnPointerClick(PointerEventData eventData)
    {
        OnClick?.Invoke();
    }
}