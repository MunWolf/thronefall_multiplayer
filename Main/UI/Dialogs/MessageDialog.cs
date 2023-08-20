using ThronefallMP.Patches;
using ThronefallMP.UI.Controls;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ThronefallMP.UI.Dialogs;

public class MessageDialog : BaseUI
{
    public override string Name => "Message Dialog";

    public delegate void ClickDelegate();

    public ClickDelegate OnClick;
    
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

    public string ButtonText
    {
        get => _button.Text.text;
        set => _button.Text.text = value;
    }

    public Color Color
    {
        get => _message.color;
        set => _message.color = value;
    }

    private TextMeshProUGUI _title;
    private TextMeshProUGUI _message;
    private GameObject _background;
    private ButtonControl _button;
    
    public override void ConstructPanelContent()
    {
        _background = UIHelper.CreateUIObject("background", PanelRoot);
        {
            var image = _background.AddComponent<Image>();
            image.type = Image.Type.Sliced;
            image.color = UIManager.TransparentBackgroundColor;
            var rectTransform = _background.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(1, 1);
        }
        
        var panelBorders = UIHelper.CreateUIObject("panel", PanelRoot);
        {
            var image = panelBorders.AddComponent<Image>();
            image.type = Image.Type.Sliced;
            image.color = UIManager.DarkBackgroundColor;
            UIHelper.SetLayoutGroup<VerticalLayoutGroup>(
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
            // TODO: Change this to fit content automatically.
            var rectTransform = panelBorders.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.35f, 0.3f);
            rectTransform.anchorMax = new Vector2(0.65f, 0.7f);
        }
        
        var panel = UIHelper.CreateUIObject("panel", panelBorders);
        {
            var image = panel.AddComponent<Image>();
            image.type = Image.Type.Sliced;
            image.color = UIManager.BackgroundColor;
            UIHelper.SetLayoutGroup<VerticalLayoutGroup>(
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

        var titleContainer = UIHelper.CreateUIObject("titleContainer", panel);
        {
            UIHelper.SetLayoutGroup<HorizontalLayoutGroup>(
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
            UIHelper.SetLayoutElement(titleContainer, ignoreLayout: true);
            var rectTransform = titleContainer.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.0f, 0.7f);
            rectTransform.anchorMax = new Vector2(1.0f, 0.9f);
        }
        
        _title = UIHelper.CreateText(titleContainer, "title", "Title");
        _title.fontSize = 36;
        _title.alignment = TextAlignmentOptions.Center;
        
        _message = UIHelper.CreateText(panel, "message", "Message");
        _message.alignment = TextAlignmentOptions.Center;

        var buttons = UIHelper.CreateUIObject("buttons", panel);
        {
            UIHelper.SetLayoutGroup<HorizontalLayoutGroup>(
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
            UIHelper.SetLayoutElement(buttons, ignoreLayout: true);
            var rectTransform = buttons.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.0f, 0.1f);
            rectTransform.anchorMax = new Vector2(1.0f, 0.3f);
        }
        
        _button = UIHelper.CreateButton(buttons, "button", "Close");
        UIHelper.SetLayoutElement(_button.gameObject, minWidth: 100);
        _button.OnClick += () =>
        {
            OnClick?.Invoke();
            Destroy(gameObject);
        };
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelBorders.GetComponent<RectTransform>());
    }

    public void OnEnable()
    {
        ++UIFramePatch.DisableGameUIInputCount;
        if (_button != null)
        {
            _button.Button.Select();
        }
    }

    public void OnDisable()
    {
        --UIFramePatch.DisableGameUIInputCount;
    }
}