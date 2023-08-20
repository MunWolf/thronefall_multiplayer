using ThronefallMP.UI.Controls;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ThronefallMP.UI;

public static class UIHelper
{
    public static GameObject CreateBox(GameObject root, string name, Color color)
    {
        var border = CreateUIObject(name, root);
        SetLayoutGroup<HorizontalLayoutGroup>(
            border,
            childControlWidth: true,
            childControlHeight: true
        );
        var image = border.AddComponent<Image>();
        image.type = Image.Type.Sliced;
        image.color = color;
        return border;
    }
    
    public static TextMeshProUGUI CreateText(GameObject root, string name, string text, TMP_FontAsset font = null)
    {
        var textObject = CreateUIObject(name, root);
        var textComponent = textObject.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.color = UIManager.TextColor;
        textComponent.font = font ? font : UIManager.DefaultFont;
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
        var buttonObject = CreateUIObject(name, root, new Vector2(25f, 25f));
        var textObject = CreateUIObject("text", buttonObject);
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
        
        SetLayoutElement(buttonObject, minWidth: 20, minHeight: 20);
        
        buttonObject.AddComponent<EventTrigger>();
        return buttonObject.AddComponent<ButtonControl>();
    }

    public static GameObject CreateCheck(GameObject root, string name, bool startsOn = false)
    {
        var toggleObject = CreateUIObject(name, root, new Vector2(25f, 25f));
        var toggle = toggleObject.AddComponent<Toggle>();
        toggleObject.AddComponent<Image>().color = Color.clear;
        toggle.isOn = startsOn;
        SetLayoutGroup<HorizontalLayoutGroup>(
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
        
        var checkBorderObject = CreateUIObject("checkmark", toggleObject);
        SetLayoutElement(checkBorderObject, flexibleWidth: 0, flexibleHeight: 0);
        var checkBackgroundObject = CreateUIObject("background", checkBorderObject).GetComponent<RectTransform>();
        checkBackgroundObject.anchorMin = new Vector2(0.1f, 0.1f);
        checkBackgroundObject.anchorMax = new Vector2(0.9f, 0.9f);
        var checkObject = CreateUIObject("check", checkBorderObject).GetComponent<RectTransform>();
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
        var textObject = CreateUIObject("text", toggleObject);
        var textComponent = textObject.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.font = UIManager.DefaultFont;
        textComponent.alignment = TextAlignmentOptions.Left;
        var fitter = textObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        SetLayoutElement(toggleObject, minWidth: 20, minHeight: 20);
        
        toggleObject.AddComponent<EventTrigger>();
        return toggleObject.AddComponent<ToggleControl>();
    }
    
    public static ToggleControl CreateRightToggle(GameObject root, string name, string text, bool startsOn = false)
    {
        var toggleObject = CreateCheck(root, name, startsOn);
        var textObject = CreateUIObject("text", toggleObject);
        var textComponent = textObject.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.font = UIManager.DefaultFont;
        textComponent.alignment = TextAlignmentOptions.Left;
        var fitter = textObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        textObject.transform.SetSiblingIndex(0);

        SetLayoutElement(toggleObject, minWidth: 20, minHeight: 20);
        
        toggleObject.AddComponent<EventTrigger>();
        return toggleObject.AddComponent<ToggleControl>();
    }
    
    public static ToggleControl CreateToggle(GameObject root, string name, bool startsOn = false)
    {
        var toggleObject = CreateCheck(root, name, startsOn);

        SetLayoutElement(toggleObject, minWidth: 20, minHeight: 20);
        
        toggleObject.AddComponent<EventTrigger>();
        return toggleObject.AddComponent<ToggleControl>();
    }
    
    public static TMP_InputField CreateInputField(GameObject panel, string name, string label, string value,
        int? labelWidth = null, int limit = 32, TMP_FontAsset font = null)
    {
        var group = CreateUIObject($"{name}Group", panel);
        SetLayoutGroup<HorizontalLayoutGroup>(
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
        SetLayoutElement(group, flexibleWidth: 1);

        if (label != null)
        {
            var bg = CreateBox(group, $"{name}_label_bg", Color.clear);
            SetLayoutGroup<HorizontalLayoutGroup>(
                bg,
                childControlWidth: true,
                childAlignment: TextAnchor.MiddleLeft
            );
            SetLayoutElement(bg.gameObject, minWidth: labelWidth, flexibleWidth: 0);
            bg.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var labelText = CreateText(bg, $"{name}_label", $"{label}: ", font);
            labelText.alignment = TextAlignmentOptions.Left;
        }

        {
            var bg = CreateBox(group, $"{name}_bg",  new Color(0.2f, 0.2f, 0.2f));
            SetLayoutElement(bg, flexibleWidth: 1);
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
            
            var text = CreateText(textArea, $"{name}", $"{value}", font);
            text.color = UIManager.TextColor;
            text.fontSize = 20;
            inputField.textComponent = text;
            inputField.text = text.text;
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
    
    public static T SetLayoutGroup<T>(
        GameObject gameObject,
        bool? forceWidth = null,
        bool? forceHeight = null,
        bool? childControlWidth = null,
        bool? childControlHeight = null,
        int? spacing = null,
        int? padTop = null,
        int? padBottom = null,
        int? padLeft = null,
        int? padRight = null,
        TextAnchor? childAlignment = null)
        where T : HorizontalOrVerticalLayoutGroup
    {
        var group = gameObject.GetComponent<T>();
        if (!(bool)(Object)group)
        {
            group = gameObject.AddComponent<T>();
        }
        return SetLayoutGroup<T>(group, forceWidth, forceHeight, childControlWidth, childControlHeight, spacing, padTop, padBottom, padLeft, padRight, childAlignment);
    }
    
    public static T SetLayoutGroup<T>(
        T group,
        bool? forceWidth = null,
        bool? forceHeight = null,
        bool? childControlWidth = null,
        bool? childControlHeight = null,
        int? spacing = null,
        int? padTop = null,
        int? padBottom = null,
        int? padLeft = null,
        int? padRight = null,
        TextAnchor? childAlignment = null)
        where T : HorizontalOrVerticalLayoutGroup
    {
        if (forceWidth.HasValue)
        {
            group.childForceExpandWidth = forceWidth.Value;
        }
        if (forceHeight.HasValue)
        {
            group.childForceExpandHeight = forceHeight.Value;
        }
        if (childControlWidth.HasValue)
        {
            group.SetChildControlWidth(childControlWidth.Value);
        }
        if (childControlHeight.HasValue)
        {
            group.SetChildControlHeight(childControlHeight.Value);
        }
        if (spacing.HasValue)
        {
            group.spacing = spacing.Value;
        }
        if (padTop.HasValue)
        {
            group.padding.top = padTop.Value;
        }
        if (padBottom.HasValue)
        {
            group.padding.bottom = padBottom.Value;
        }
        if (padLeft.HasValue)
        {
            group.padding.left = padLeft.Value;
        }
        if (padRight.HasValue)
        {
            group.padding.right = padRight.Value;
        }
        if (childAlignment.HasValue)
        {
            group.childAlignment = childAlignment.Value;
        }
        
        return group;
    }
    
    public static GameObject CreateUIObject(string name, GameObject parent, Vector2 sizeDelta = default (Vector2))
    {
        var gameObject = new GameObject(name)
        {
            layer = 5,
            hideFlags = HideFlags.HideAndDontSave
        };

        if ((bool)(Object)parent)
        {
            gameObject.transform.SetParent(parent.transform, false);
        }
        
        gameObject.AddComponent<RectTransform>().sizeDelta = sizeDelta;
        return gameObject;
    }
    
    public static LayoutElement SetLayoutElement(GameObject gameObject, int? minWidth = null, int? minHeight = null,
        int? flexibleWidth = null, int? flexibleHeight = null, int? preferredWidth = null, int? preferredHeight = null,
        bool? ignoreLayout = null)
    {
        var layout = gameObject.GetComponent<LayoutElement>();
        if (!layout)
        {
            layout = gameObject.AddComponent<LayoutElement>();
        }

        if (minWidth != null)
        {
            layout.minWidth = minWidth.Value;
        }

        if (minHeight != null)
        {
            layout.minHeight = minHeight.Value;
        }

        if (flexibleWidth != null)
        {
            layout.flexibleWidth = flexibleWidth.Value;
        }

        if (flexibleHeight != null)
        {
            layout.flexibleHeight = flexibleHeight.Value;
        }

        if (preferredWidth != null)
        {
            layout.preferredWidth = preferredWidth.Value;
        }

        if (preferredHeight != null)
        {
            layout.preferredHeight = preferredHeight.Value;
        }

        if (ignoreLayout != null)
        {
            layout.ignoreLayout = ignoreLayout.Value;
        }

        return layout;
    }
    
    public static void SetChildControlHeight(this HorizontalOrVerticalLayoutGroup group, bool value) => group.childControlHeight = value;
    public static void SetChildControlWidth(this HorizontalOrVerticalLayoutGroup group, bool value) => group.childControlWidth = value;
}