using Rewired;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ThronefallMP.UI.Controls;

public class CustomScrollRect : ScrollRect
{
    private bool _swallowMouseWheelScrolls = true;
    private void Update()
    {
        var value = ReInput.players.GetPlayer(0)?.controllers?.Mouse?.Axes[2]?.value;
        if (!value.HasValue)
        {
            return;
        }
        
        var pointerData = new PointerEventData(EventSystem.current)
        {
            scrollDelta = new Vector2(0, value.Value)
        };

        _swallowMouseWheelScrolls = false;
        OnScroll(pointerData);
        _swallowMouseWheelScrolls = true;
    }
 
    public override void OnScroll(PointerEventData data)
    {
        if (!_swallowMouseWheelScrolls)
        {
            base.OnScroll(data);
        }
    }
}
