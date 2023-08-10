using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;

namespace ThronefallMP.UI.Controls;

public class ButtonControl : MonoBehaviour
{
    public struct Style
    {
        public Color Color;
        public float Size;
    }
    
    public delegate void ButtonEvent();

    public event ButtonEvent OnSelected;
    public event ButtonEvent OnDeselected;
    public event ButtonEvent OnEnter;
    public event ButtonEvent OnExit;
    public event ButtonEvent OnClick;

    public Button Button { get; private set; }
    public TextMeshProUGUI Text { get; private set; }
    public bool Selected { get; private set; }
    public float SizeChangeTime { get; set; } = 0.1f;

    public Style Normal = new() { Color = UIManager.ButtonTextColor, Size = 30 };
    public Style Hover = new() { Color = UIManager.ButtonHoverTextColor, Size = 40 };
    public Style NormalSelected = new() { Color = UIManager.ButtonTextColor, Size = 40 };
    public Style HoverSelected = new() { Color = UIManager.ButtonHoverTextColor, Size = 40 };
    public Style Noninteractive = new() { Color = UIManager.NoninteractiveButtonTextColor, Size = 30 };

    public Selectable NavLeft
    {
        get => Button.navigation.selectOnLeft;
        set => Button.navigation = Button.navigation with { selectOnLeft = value };
    }

    public Selectable NavRight
    {
        get => Button.navigation.selectOnRight;
        set => Button.navigation = Button.navigation with { selectOnRight = value };
    }

    public Selectable NavUp
    {
        get => Button.navigation.selectOnUp;
        set => Button.navigation = Button.navigation with { selectOnUp = value };
    }

    public Selectable NavDown
    {
        get => Button.navigation.selectOnDown;
        set => Button.navigation = Button.navigation with { selectOnDown = value };
    }
    
    private bool _hovering;
    private float _startSize;
    private float _endSize;
    private float _lerpSize;

    public void SetInteractable(bool value)
    {
        if (value == Button.interactable)
        {
            return;
        }

        Button.interactable = value;
        Apply(value ? Normal : Noninteractive);
    }
    
    private void Awake()
    {
        Button = GetComponent<Button>();
        Text = GetComponentInChildren<TextMeshProUGUI>();
        Button.navigation = Button.navigation with { mode = Navigation.Mode.Explicit };
        _startSize = Normal.Size;
        _endSize = Normal.Size;
        _lerpSize = 1.0f;
        Apply(Button.interactable ? Normal : Noninteractive, true);
        UIHelper.AddEvent(gameObject, EventTriggerType.PointerEnter, (_) =>
        {
            if (!Button.interactable)
            {
                return;
            }

            _hovering = true;
            Apply(Selected ? HoverSelected : Hover);
            OnEnter?.Invoke();
        });
        UIHelper.AddEvent(gameObject, EventTriggerType.PointerExit, (_) =>
        {
            if (!Button.interactable)
            {
                return;
            }

            _hovering = false;
            Apply(Selected ? NormalSelected : Normal);
            OnExit?.Invoke();
        });
        UIHelper.AddEvent(gameObject, EventTriggerType.Select, (_) =>
        {
            if (!Button.interactable)
            {
                return;
            }
            
            Selected = true;
            Apply(_hovering ? HoverSelected : NormalSelected);
            OnSelected?.Invoke();
        });
        UIHelper.AddEvent(gameObject, EventTriggerType.Deselect, (_) =>
        {
            if (!Button.interactable)
            {
                return;
            }

            Selected = false;
            Apply(_hovering ? Hover : Normal);
            OnDeselected?.Invoke();
        });
        UIHelper.AddEvent(gameObject, EventTriggerType.PointerClick, (_) =>
        {
            if (!Button.interactable)
            {
                return;
            }

            OnClick?.Invoke();
        });
        UIHelper.AddEvent(gameObject, EventTriggerType.Submit, (_) =>
        {
            if (!Button.interactable)
            {
                return;
            }

            OnClick?.Invoke();
        });
    }

    private void Update()
    {
        if (Math.Abs(_startSize - _endSize) < 0.01f)
        {
            return;
        }

        _lerpSize = Math.Min(_lerpSize + Time.deltaTime / SizeChangeTime, 1.0f);
        Text.fontSize = Lerp.Interpolate(_startSize, _endSize, _lerpSize);
    }

    public void Reset()
    {
        _hovering = false;
        Selected = false;
        Apply(Button.interactable ? Normal : Noninteractive, true);
    }
    
    private void OnDisable()
    {
        Reset();
    }

    private void Apply(Style style, bool instant = false, bool resetLerp = true)
    {
        Text.color = style.Color;
        _endSize = style.Size;
        if (!instant)
        {
            _startSize = Text.fontSize;
            if (resetLerp)
            {
                _lerpSize = 1.0f - _lerpSize;
            }
        }
        else
        {
            _lerpSize = 1.0f;
            _startSize = _endSize;
            Text.fontSize = _endSize;
        }
    }
}