using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using UniverseLib.UI;

namespace ThronefallMP.UI.Panels;

public class ChatPanel : BaseUI
{
    private class Message
    {
        public GameObject Line;
        public TMP_Text Text;
        public float Time;
        public bool Faded;
    }
    
    // TODO: Don't throw away messages, use a scroll rect
    private const int MaxLines = 8;
    private const float MessageTimeout = 3f;
    private const float MessageFadeTime = 0.5f;
    
    private static readonly Color BackgroundColor = new(0.07f, 0.07f, 0.07f, 1f);
    private static readonly Color InputColor = new(0.9f, 0.9f, 0.9f, 1f);
    private static readonly Color ChatColor = new(0.9f, 0.9f, 0.9f, 1f);
    private static readonly Color32 ChatBorderColor = new(0, 0, 0, 255);
    
    public override string Name => "Game Status Panel";

    private GameObject _container;
    private GameObject _chatBoxContainer;
    private TMP_InputField _chatBox;
    private readonly List<Message> _lines = new();
    private TMP_FontAsset _chatBoxFont;
    private TMP_FontAsset _chatFont;
    private bool _justClosed;

    private Font CreateFont()
    {
        Dictionary<string, int> fonts = new()
        {
            { "arial", 0 },
            { "times", 1 },
            { "verdana", 2 },
            { "cour", 3 }
        };

        var currentPriority = int.MaxValue;
        var font = Font.GetPathsToOSFonts()[0];
        foreach (var path in Font.GetPathsToOSFonts())
        {
            var fontName = Path.GetFileNameWithoutExtension(path);
            if (!fonts.TryGetValue(fontName, out var priority) || priority >= currentPriority)
            {
                continue;
            }
            
            font = path;
            currentPriority = priority;
            if (currentPriority == 0)
            {
                break;
            }
        }

        Plugin.Log.LogInfo($"Loaded font '{font}' for chat messages.");
        return new Font(font);
    }
    
    public override void ConstructPanelContent()
    {
        var font = CreateFont();
        _chatBoxFont = TMP_FontAsset.CreateFontAsset(
            font,
            90,
            9,
            GlyphRenderMode.SDFAA,
            1024,
            1024
        );
        
        _chatFont = TMP_FontAsset.CreateFontAsset(
            font,
            90,
            9,
            GlyphRenderMode.SDFAA,
            1024,
            1024
        );
        ShaderUtilities.GetShaderPropertyIDs();
        _chatFont.material.EnableKeyword(ShaderUtilities.Keyword_Outline);
        _chatFont.material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.3f);
        
        _container = UIFactory.CreateUIObject("container", PanelRoot);
        {
            var rectTransform = _container.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0.5f);
            rectTransform.anchorMax = new Vector2(0.35f, 0.8f);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(
                _container,
                true,
                false,
                true,
                true,
                0,
                10,
                10,
                10,
                10,
                TextAnchor.LowerLeft
            );
        }

        var chatBoxBounds = UIFactory.CreateUIObject("chat_box_container", PanelRoot);
        {
            var rectTransform = chatBoxBounds.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.4f, 0.4f);
            rectTransform.anchorMax = new Vector2(0.6f, 0.5f);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(
                chatBoxBounds,
                true,
                false,
                true,
                true,
                0,
                0,
                0,
                0,
                0,
                TextAnchor.MiddleCenter
            );
        }
        
        _chatBox = UIHelper.CreateInputField(chatBoxBounds, "chat_box", null, "", font: _chatBoxFont);
        _chatBox.gameObject.AddComponent<Mask>();
        _chatBox.textComponent.color = InputColor;
        _chatBox.textComponent.alignment = TextAlignmentOptions.Left;
        _chatBox.characterLimit = 0;
        _chatBox.onSubmit.AddListener(OnSendMessage);
        _chatBoxContainer = _chatBox.transform.parent.gameObject;
        {
            var image = _chatBoxContainer.AddComponent<Image>();
            image.type = Image.Type.Sliced;
            image.color = BackgroundColor;
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(
                _chatBoxContainer,
                true,
                false,
                true,
                false,
                0,
                3,
                3,
                3,
                3,
                TextAnchor.MiddleCenter
            );
            var rectTransform = _chatBoxContainer.GetComponent<RectTransform>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            _chatBoxContainer.SetActive(false);
        }

        Plugin.Instance.Network.OnReceivedChatMessage += OnMessageReceived;
    }

    public void Update()
    {
        if (!Plugin.Instance.Network.Online)
        {
            foreach (var data in _lines)
            {
                Destroy(data.Line);
            }
            
            _lines.Clear();
            return;
        }
        
        foreach (var data in _lines)
        {
            if (data.Faded && _chatBoxContainer.activeSelf)
            {
                data.Time = Time.unscaledTime;
                data.Text.CrossFadeAlpha(1, 0, true);
                data.Faded = false;
            }
            else if (!data.Faded && data.Time + MessageTimeout < Time.unscaledTime)
            {
                data.Text.CrossFadeAlpha(0, MessageFadeTime, true);
                data.Faded = true;
            }
        }

        switch (_chatBoxContainer.activeSelf)
        {
            case false when _justClosed:
                _justClosed = Input.GetKeyDown(KeyCode.Return);
                break;
            case false when Input.GetKeyDown(KeyCode.Return):
                _chatBoxContainer.SetActive(true);
                LocalGamestate.Instance.SetPlayerFreezeState(true);
                break;
            case true when !_chatBox.isFocused:
                _chatBox.Select();
                _chatBox.ActivateInputField();
                break;
        }
    }

    private void OnSendMessage(string text)
    {
        Plugin.Instance.Network.SendChatMessage(text);
        _chatBox.text = "";
        _chatBoxContainer.SetActive(false);
        LocalGamestate.Instance.SetPlayerFreezeState(false);
        _justClosed = true;
    }

    private void OnMessageReceived(string user, string message)
    {
        while (_lines.Count >= MaxLines)
        {
            var data = _lines[0];
            _lines.RemoveAt(0);
            Destroy(data.Line);
        }
        
        var text = UIHelper.CreateText(_container, "message", $"{user}: {message}", _chatFont);
        text.alignment = TextAlignmentOptions.Left;
        text.color = ChatColor;
        text.fontSize = 20;
        text.outlineColor = ChatBorderColor;
        text.outlineWidth = 0.3f;

        _lines.Add(new Message { Line = text.gameObject, Text = text, Time = Time.unscaledTime });
    }
}