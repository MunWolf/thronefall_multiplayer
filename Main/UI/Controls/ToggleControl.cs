using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;

namespace ThronefallMP.UI.Controls;

public class ToggleControl : MonoBehaviour
{
    public struct Style
    {
        public Color Color;
        public Color BackgroundColor;
        public float FontSize;

        public Style()
        {
            Color = UIManager.ButtonTextColor;
            BackgroundColor = UIManager.BackgroundColor;
            FontSize = 20;
        }
    }
    
    public delegate void ToggleEvent();

    public event ToggleEvent OnSelected;
    public event ToggleEvent OnDeselected;
    public event ToggleEvent OnEnter;
    public event ToggleEvent OnExit;
    public event ToggleEvent OnToggledOn;
    public event ToggleEvent OnToggledOff;

    public Toggle Toggle { get; private set; }
    public TextMeshProUGUI Text { get; private set; }
    public bool Selected { get; private set; }

    public Selectable NavLeft
    {
        get => Toggle.navigation.selectOnLeft;
        set => Toggle.navigation = Toggle.navigation with { selectOnLeft = value };
    }

    public Selectable NavRight
    {
        get => Toggle.navigation.selectOnRight;
        set => Toggle.navigation = Toggle.navigation with { selectOnRight = value };
    }

    public Selectable NavUp
    {
        get => Toggle.navigation.selectOnUp;
        set => Toggle.navigation = Toggle.navigation with { selectOnUp = value };
    }

    public Selectable NavDown
    {
        get => Toggle.navigation.selectOnDown;
        set => Toggle.navigation = Toggle.navigation with { selectOnDown = value };
    }

    public int CheckmarkSize
    {
        get => _checkmarkSize;
        set {
            UIHelper.SetLayoutElement(_checkBorder, minWidth: value, minHeight: value);
            _checkmarkSize = value;
        }
    }

    public Style Normal = new() { Color = UIManager.ButtonTextColor, BackgroundColor = UIManager.BackgroundColor };
    public Style Hover = new() { Color = UIManager.ButtonHoverTextColor, BackgroundColor = UIManager.BackgroundColor };
    public Style NormalSelected = new() { Color = UIManager.ButtonTextColor, BackgroundColor = UIManager.BackgroundColor };
    public Style HoverSelected = new() { Color = UIManager.ButtonHoverTextColor, BackgroundColor = UIManager.BackgroundColor };
    public Style Noninteractive = new() { Color = UIManager.NoninteractiveButtonTextColor, BackgroundColor = UIManager.BackgroundColor };

    private int _checkmarkSize;
    private GameObject _checkBorder;
    private GameObject _background;
    private GameObject _check;
    private bool _hovering;

    public void SetInteractable(bool value)
    {
        if (value == Toggle.interactable)
        {
            return;
        }

        Toggle.interactable = value;
        Apply(value ? Normal : Noninteractive);
    }
    
    private void Awake()
    {
        Toggle = GetComponent<Toggle>();
        Text = transform.GetComponentInChildren<TextMeshProUGUI>();
        Toggle.navigation = Toggle.navigation with { mode = Navigation.Mode.Explicit };
        _checkBorder = transform.Find("checkmark").gameObject;
        _background = transform.Find("checkmark/background").gameObject;
        _check = transform.Find("checkmark/check").gameObject;
        _check.SetActive(Toggle.isOn);
        Apply(Normal);
        CheckmarkSize = 20;
        UIHelper.AddEvent(gameObject, EventTriggerType.PointerEnter, (_) =>
        {
            if (!Toggle.interactable)
            {
                return;
            }

            _hovering = true;
            Apply(Selected ? HoverSelected : Hover);
            OnEnter?.Invoke();
        });
        UIHelper.AddEvent(gameObject, EventTriggerType.PointerExit, (_) =>
        {
            if (!Toggle.interactable)
            {
                return;
            }

            _hovering = false;
            Apply(Selected ? NormalSelected : Normal);
            OnExit?.Invoke();
        });
        UIHelper.AddEvent(gameObject, EventTriggerType.Select, (_) =>
        {
            if (!Toggle.interactable)
            {
                return;
            }
            
            Selected = true;
            Apply(_hovering ? HoverSelected : NormalSelected);
            OnSelected?.Invoke();
        });
        UIHelper.AddEvent(gameObject, EventTriggerType.Deselect, (_) =>
        {
            if (!Toggle.interactable)
            {
                return;
            }

            Selected = false;
            Apply(_hovering ? Hover : Normal);
            OnDeselected?.Invoke();
        });
        Toggle.onValueChanged.AddListener((value) =>
        {
            if (!Toggle.interactable)
            {
                return;
            }

            _check.SetActive(value);
            (value ? OnToggledOn : OnToggledOff)?.Invoke();
        });
    }

    private void OnDisable()
    {
        _hovering = false;
        Selected = false;
        Apply(Normal);
    }

    private void Apply(Style style)
    {
        if (Text != null)
        {
            Text.color = style.Color;
            Text.fontSize = style.FontSize;
        }
        
        _checkBorder.GetComponent<Image>().color = style.Color;
        _background.GetComponent<Image>().color = style.BackgroundColor;
        _check.GetComponent<Image>().color = style.Color;
    }
}