using ThronefallMP.UI.Controls;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UniverseLib.UI;

namespace ThronefallMP.UI;

public static class UIHelper
{
    public static GameObject CreateBox(GameObject root, string name, Color color)
    {
        var border = UIFactory.CreateUIObject(name, root);
        UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(
            border,
            childControlWidth: true,
            childControlHeight: true
        );
        var image = border.AddComponent<Image>();
        image.type = Image.Type.Sliced;
        image.color = color;
        return border;
    }
    
    public static TextMeshProUGUI CreateText(GameObject root, string name, string text)
    {
        var textObject = UIFactory.CreateUIObject(name, root);
        var textComponent = textObject.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.color = UIManager.ButtonTextColor;
        textComponent.font = UIManager.DefaultFont;
        textComponent.fontSize = 24;
        textComponent.alignment = TextAlignmentOptions.Center;
        
        var component = textObject.GetComponent<RectTransform>();
        component.anchorMin = Vector2.zero;
        component.anchorMax = Vector2.one;
        component.sizeDelta = Vector2.zero;
        
        return textComponent;
    }
    
    public static ButtonControl CreateButton(GameObject root, string name, string text)
    {
        var buttonObject = UIFactory.CreateUIObject(name, root, new Vector2(25f, 25f));
        var textObject = UIFactory.CreateUIObject("text", buttonObject);
        var button = buttonObject.AddComponent<Button>();
        var navigation = button.navigation with { mode = Navigation.Mode.Explicit };
        button.navigation = navigation;
        
        var textComponent = textObject.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.font = UIManager.DefaultFont;
        textComponent.alignment = TextAlignmentOptions.Center;
        
        var component = textObject.GetComponent<RectTransform>();
        component.anchorMin = Vector2.zero;
        component.anchorMax = Vector2.one;
        component.sizeDelta = Vector2.zero;
        button.onClick.AddListener(() => button.OnDeselect(null));
        
        UIFactory.SetLayoutElement(buttonObject, minWidth: 20, minHeight: 20);
        
        buttonObject.AddComponent<EventTrigger>();
        return buttonObject.AddComponent<ButtonControl>();
    }

    public static GameObject CreateCheck(GameObject root, string name, bool startsOn = false)
    {
        var toggleObject = UIFactory.CreateUIObject(name, root, new Vector2(25f, 25f));
        var toggle = toggleObject.AddComponent<Toggle>();
        toggleObject.AddComponent<Image>().color = Color.clear;
        toggle.isOn = startsOn;
        UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(
            toggleObject,
            false,
            false,
            true,
            true,
            10,
            0,
            0,
            0,
            0,
            TextAnchor.MiddleLeft
        );
        var navigation = toggle.navigation with { mode = Navigation.Mode.Explicit };
        toggle.navigation = navigation;
        
        var checkBorderObject = UIFactory.CreateUIObject("checkmark", toggleObject);
        UIFactory.SetLayoutElement(checkBorderObject, flexibleWidth: 0, flexibleHeight: 0);
        var checkBackgroundObject = UIFactory.CreateUIObject("background", checkBorderObject).GetComponent<RectTransform>();
        checkBackgroundObject.anchorMin = new Vector2(0.1f, 0.1f);
        checkBackgroundObject.anchorMax = new Vector2(0.9f, 0.9f);
        var checkObject = UIFactory.CreateUIObject("check", checkBorderObject).GetComponent<RectTransform>();
        checkObject.anchorMin = new Vector2(0.2f, 0.2f);
        checkObject.anchorMax = new Vector2(0.8f, 0.8f);

        var image = checkBorderObject.gameObject.AddComponent<Image>();
        image.type = Image.Type.Sliced;
        image = checkBackgroundObject.gameObject.AddComponent<Image>();
        image.type = Image.Type.Sliced;
        image = checkObject.gameObject.AddComponent<Image>();
        image.type = Image.Type.Sliced;

        return toggleObject;
    }
    
    public static ToggleControl CreateLeftToggle(GameObject root, string name, string text, bool startsOn = false)
    {
        var toggleObject = CreateCheck(root, name, startsOn);
        var textObject = UIFactory.CreateUIObject("text", toggleObject);
        var textComponent = textObject.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.font = UIManager.DefaultFont;
        textComponent.alignment = TextAlignmentOptions.Left;
        var fitter = textObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        UIFactory.SetLayoutElement(toggleObject, minWidth: 20, minHeight: 20);
        
        toggleObject.AddComponent<EventTrigger>();
        return toggleObject.AddComponent<ToggleControl>();
    }
    
    public static ToggleControl CreateRightToggle(GameObject root, string name, string text, bool startsOn = false)
    {
        var toggleObject = CreateCheck(root, name, startsOn);
        var textObject = UIFactory.CreateUIObject("text", toggleObject);
        var textComponent = textObject.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.font = UIManager.DefaultFont;
        textComponent.alignment = TextAlignmentOptions.Left;
        var fitter = textObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        textObject.transform.SetSiblingIndex(0);

        UIFactory.SetLayoutElement(toggleObject, minWidth: 20, minHeight: 20);
        
        toggleObject.AddComponent<EventTrigger>();
        return toggleObject.AddComponent<ToggleControl>();
    }
    
    public static ToggleControl CreateToggle(GameObject root, string name, bool startsOn = false)
    {
        var toggleObject = CreateCheck(root, name, startsOn);

        UIFactory.SetLayoutElement(toggleObject, minWidth: 20, minHeight: 20);
        
        toggleObject.AddComponent<EventTrigger>();
        return toggleObject.AddComponent<ToggleControl>();
    }

    public static void AddEvent(GameObject button, EventTriggerType type, UnityAction<BaseEventData> action)
    {
        var trigger = button.GetComponent<EventTrigger>();
        var entry = new EventTrigger.TriggerEvent();
        entry.AddListener(action);
        
        trigger.triggers.Add(new EventTrigger.Entry()
        {
            eventID = type,
            callback = entry
        });
    }
}