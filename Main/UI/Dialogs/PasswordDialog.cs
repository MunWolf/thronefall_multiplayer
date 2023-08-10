using System;
using ThronefallMP.Patches;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib.UI;
using Object = UnityEngine.Object;

namespace ThronefallMP.UI.Dialogs;

public class PasswordDialog : BaseUI
{
    public override string Name => "Password Dialog";

    public bool FadeScreen
    {
        get => _background.activeSelf;
        set => _background.SetActive(value);
    }

    public delegate void Confirm(string password);
    public delegate void Cancel();

    public Confirm OnConfirm;
    public Cancel OnCancel;

    private GameObject _background;
    private TMP_InputField _input;
    
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
            rectTransform.anchorMin = new Vector2(0.35f, 0.4f);
            rectTransform.anchorMax = new Vector2(0.65f, 0.6f);
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
        
        var title = UIHelper.CreateText(titleContainer, "title", "Password");
        title.fontSize = 36;
        title.alignment = TextAlignmentOptions.Center;

        _input = UIHelper.CreateInputField(panel, "password", null, "", 24);

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
        
        var connect = UIHelper.CreateButton(buttons, "connect", "Connect");
        UIFactory.SetLayoutElement(connect.gameObject, minWidth: 120);
        connect.OnClick += () =>
        {
            Destroy(gameObject);
            OnConfirm?.Invoke(_input.text);
        };
        
        var cancel = UIHelper.CreateButton(buttons, "cancel", "Cancel");
        UIFactory.SetLayoutElement(cancel.gameObject, minWidth: 120);
        cancel.OnClick += () =>
        {
            Destroy(gameObject);
            OnCancel?.Invoke();
        };
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelBorders.GetComponent<RectTransform>());
    }

    public void OnEnable()
    {
        ++UIFramePatch.DisableGameUIInputCount;
        if (_input != null)
        {
            _input.text = "";
        }
    }

    public void OnDisable()
    {
        --UIFramePatch.DisableGameUIInputCount;
    }
}