using System;
using ThronefallMP.Patches;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib.UI;

namespace ThronefallMP.UI.Dialogs;

public class MessageDialog : BaseUI
{
    public override string Name => "Message Dialog";

    public bool FadeScreen
    {
        get => _background.activeSelf;
        set => _background.SetActive(value);
    }

    public string Title
    {
        get => _title.text;
        set => _title.text = value;
    }

    public string Message
    {
        get => _message.text;
        set => _message.text = value;
    }

    public Color Color
    {
        get => _message.color;
        set => _message.color = value;
    }

    private TextMeshProUGUI _title;
    private TextMeshProUGUI _message;
    private GameObject _background;
    
    public override void ConstructPanelContent()
    {
        _background = UIFactory.CreateUIObject("background", PanelRoot);
        {
            var image = _background.AddComponent<Image>();
            image.type = Image.Type.Sliced;
            image.color = UIManager.TransparentBackgroundColor;
            var rectTransform = _background.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(1, 1);
        }
        
        var panelBorders = UIFactory.CreateUIObject("panel", PanelRoot);
        {
            var image = panelBorders.AddComponent<Image>();
            image.type = Image.Type.Sliced;
            image.color = UIManager.DarkBackgroundColor;
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(
                panelBorders,
                true,
                true,
                true,
                true,
                0,
                5,
                5,
                5,
                5,
                TextAnchor.MiddleLeft
            );
            var rectTransform = panelBorders.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.35f, 0.3f);
            rectTransform.anchorMax = new Vector2(0.65f, 0.7f);
        }
        
        var panel = UIFactory.CreateUIObject("panel", panelBorders);
        {
            var image = panel.AddComponent<Image>();
            image.type = Image.Type.Sliced;
            image.color = UIManager.BackgroundColor;
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(
                panel,
                false,
                false,
                true,
                true,
                5,
                20,
                20,
                60,
                60,
                TextAnchor.MiddleCenter
            );
            var rectTransform = panel.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            rectTransform.anchorMax = new Vector2(1.0f, 1.0f);
        }

        var titleContainer = UIFactory.CreateUIObject("buttons", panel);
        {
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(
                titleContainer,
                false,
                false,
                true,
                true,
                20,
                0,
                0,
                0,
                0,
                TextAnchor.MiddleCenter
            );
            UIFactory.SetLayoutElement(titleContainer, ignoreLayout: true);
            var rectTransform = titleContainer.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.0f, 0.7f);
            rectTransform.anchorMax = new Vector2(1.0f, 0.9f);
        }
        
        _title = UIHelper.CreateText(titleContainer, "title", "Title");
        _title.fontSize = 36;
        _title.alignment = TextAlignmentOptions.Center;
        
        _message = UIHelper.CreateText(panel, "message", "Message");
        _message.alignment = TextAlignmentOptions.Center;

        var buttons = UIFactory.CreateUIObject("buttons", panel);
        {
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(
                buttons,
                false,
                false,
                true,
                true,
                20,
                0,
                0,
                0,
                0,
                TextAnchor.MiddleCenter
            );
            UIFactory.SetLayoutElement(buttons, ignoreLayout: true);
            var rectTransform = buttons.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.0f, 0.1f);
            rectTransform.anchorMax = new Vector2(1.0f, 0.3f);
        }
        
        var button = UIHelper.CreateButton(buttons, "button", "Close");
        UIFactory.SetLayoutElement(button.gameObject, minWidth: 100);
        button.OnClick += () => Destroy(gameObject);;
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelBorders.GetComponent<RectTransform>());
    }

    public void OnEnable()
    {
        ++UIFramePatch.DisableGameUIInputCount;
    }

    public void OnDisable()
    {
        --UIFramePatch.DisableGameUIInputCount;
    }
}