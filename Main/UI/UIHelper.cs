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
        textComponent.color = UIManager.TextColor;
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
        checkObject.anchorMin = new Vector2(0.3f, 0.3f);
        checkObject.anchorMax = new Vector2(0.7f, 0.7f);

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
    
    public static TMP_InputField CreateInputField(GameObject panel, string name, string label, string value,
        int? labelWidth = null, int limit = 32, TMP_InputField.ContentType type = TMP_InputField.ContentType.Alphanumeric)
    {
        var group = UIFactory.CreateUIObject($"{name}Group", panel);
        UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(
            group,
            false,
            false,
            true,
            false,
            20,
            5,
            5,
            5,
            5,
            TextAnchor.MiddleLeft
        );
        UIFactory.SetLayoutElement(group, flexibleWidth: 1);

        if (label != null)
        {
            var bg = CreateBox(group, $"{name}_label_bg", Color.clear);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(
                bg,
                childControlWidth: true,
                childAlignment: TextAnchor.MiddleLeft
            );
            UIFactory.SetLayoutElement(bg.gameObject, minWidth: labelWidth, flexibleWidth: 0);
            bg.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var labelText = CreateText(bg, $"{name}_label", $"{label}: ");
            labelText.alignment = TextAlignmentOptions.Left;
        }

        {
            var bg = CreateBox(group, $"{name}_bg",  new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(bg, flexibleWidth: 1);
            bg.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var inputField = bg.AddComponent<TMP_InputField>();
            inputField.textViewport = inputField.transform.parent.GetComponent<RectTransform>();
            inputField.targetGraphic = bg.GetComponent<Image>();
        
            var textArea = new GameObject("area", typeof(RectTransform));
            inputField.textViewport = textArea.GetComponent<RectTransform>();
            textArea.transform.SetParent(bg.transform);
            inputField.textViewport.localPosition = Vector3.zero;
            inputField.textViewport.anchorMin = new Vector2(0, 0);
            inputField.textViewport.anchorMax = new Vector2(1, 1);
            textArea.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            var text = CreateText(textArea, $"{name}", $"{value}");
            text.color = UIManager.TextColor;
            text.fontSize = 20;
            inputField.textComponent = text;
            inputField.text = text.text;
            inputField.contentType = type;
            inputField.characterLimit = limit;
            var transform = text.GetComponent<RectTransform>();
            transform.localPosition = Vector3.zero;
            transform.anchorMin = new Vector2(0, 0);
            transform.anchorMax = new Vector2(1, 1);
            inputField.onFocusSelectAll = false;

            return inputField;
        }
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