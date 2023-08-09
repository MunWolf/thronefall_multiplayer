using On.I2.Loc;
using ThronefallMP.Steam;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace ThronefallMP.UI;

public class LobbyItem : MonoBehaviour, IPointerClickHandler
{
    public delegate void HandleClick();
    public event HandleClick OnClick;

    public GameObject LobbyGameObject;
    public Lobby LobbyInfo;
    
    public void OnPointerClick(PointerEventData eventData)
    {
        OnClick?.Invoke();
    }
}