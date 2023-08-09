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
        var image = border.gameObject.AddComponent<Image>();
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
        var textObject = UIFactory.CreateUIObject("Text", buttonObject);
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

    public static void AddButtonEvent(GameObject button, EventTriggerType type, UnityAction<BaseEventData> action)
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