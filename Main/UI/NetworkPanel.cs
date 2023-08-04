using ThronefallMP.NetworkPackets;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using Image = UnityEngine.UI.Image;

namespace ThronefallMP.UI;

public class NetworkPanel : UniverseLib.UI.Panels.PanelBase
{
    public override string Name => "Network Panel";
    public override int MinWidth => 0;
    public override int MinHeight => 0;
    public override Vector2 DefaultAnchorMin => new(0.43f, 0.27f);
    public override Vector2 DefaultAnchorMax => new(0.57f, 0.62f);
    public override bool CanDragAndResize => false;
    
    private static readonly Color BackgroundColor = new(0.11f, 0.11f, 0.11f, 1.0f);
    private static readonly Color TextColor = new(0.78f, 0.65f, 0.46f, 1.0f);
    private static readonly Color ButtonTextColor = new(0.97f, 0.88f, 0.75f, 1.0f);
    private static readonly Color ButtonHoverTextColor = new(0.0f, 0.41f, 0.11f);
    private static readonly Color ExitButtonColor = new(0.176f, 0.165f, 0.149f);

    public NetworkPanel(UIBase owner) : base(owner)
    {
        Rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private void AddButtonEvent(ButtonRef button, EventTriggerType type, UnityAction<BaseEventData> action)
    {
        var trigger = button.GameObject.GetComponent<EventTrigger>();
        var entry = new EventTrigger.TriggerEvent();
        entry.AddListener(action);
        
        trigger.triggers.Add(new EventTrigger.Entry()
        {
            eventID = type,
            callback = entry
        });
    }

    protected override void ConstructPanelContent()
    {
        uiRoot.GetComponent<Image>().color = BackgroundColor;
        ContentRoot.GetComponent<Image>().color = BackgroundColor;
        
        var exit = UIFactory.CreateButton(ContentRoot, "exit", "X");
        UIFactory.SetLayoutElement(exit.GameObject, minWidth: 20, minHeight: 20);
        exit.Component.image.enabled = false;
        exit.GameObject.AddComponent<EventTrigger>();
        exit.ButtonText.color = ExitButtonColor;
        AddButtonEvent(exit, EventTriggerType.PointerExit, (data) =>
        {
            exit.ButtonText.fontSize = 14;
            exit.ButtonText.color = ExitButtonColor;
        });
        AddButtonEvent(exit, EventTriggerType.PointerEnter, (data) =>
        {
            exit.ButtonText.fontSize = 16;
            exit.ButtonText.color = ButtonHoverTextColor;
        });
        AddButtonEvent(exit, EventTriggerType.Select, (data) =>
        {
            exit.ButtonText.fontSize = 14;
            exit.ButtonText.color = ExitButtonColor;
            UIManager.CloseNetworkPanel();
        });
        
        var panel = UIFactory.CreateHorizontalGroup(
            ContentRoot,
            "panel",
            true,
            true,
            true,
            true,
            childAlignment: TextAnchor.MiddleCenter,
            padding: new Vector4(10, 30, 5, 5),
            bgColor: BackgroundColor
        );
        UIFactory.SetLayoutElement(panel, flexibleWidth: 500, flexibleHeight: 600);
        panel.SetActive(false);
        
        var main = UIFactory.CreateVerticalGroup(
            panel,
            "main",
            false,
            false,
            true,
            true,
            spacing: 20,
            childAlignment: TextAnchor.MiddleCenter,
            bgColor: BackgroundColor
        );
        
        var host = UIFactory.CreateVerticalGroup(
            panel,
            "host",
            false,
            false,
            true,
            true,
            spacing: 20,
            childAlignment: TextAnchor.MiddleCenter,
            bgColor: BackgroundColor
        );
        host.SetActive(false);
        
        var connect = UIFactory.CreateVerticalGroup(
            panel,
            "connect",
            false,
            false,
            true,
            true,
            spacing: 20,
            childAlignment: TextAnchor.MiddleCenter,
            bgColor: BackgroundColor
        );
        connect.SetActive(false);
        
        // Main group
        {
            var hostButton = UIFactory.CreateButton(main, "host", "Host", BackgroundColor);
            UIFactory.SetLayoutElement(hostButton.GameObject, minWidth: 80, minHeight: 20);
            hostButton.ButtonText.color = ButtonTextColor;
            hostButton.GameObject.GetComponent<Image>().enabled = false;
            hostButton.GameObject.AddComponent<EventTrigger>();
            AddButtonEvent(hostButton, EventTriggerType.PointerExit, (data) =>
            {
                hostButton.ButtonText.fontSize = 14;
                hostButton.ButtonText.color = ButtonTextColor;
            });
            AddButtonEvent(hostButton, EventTriggerType.PointerEnter, (data) =>
            {
                hostButton.ButtonText.fontSize = 16;
                hostButton.ButtonText.color = ButtonHoverTextColor;
            });
            AddButtonEvent(hostButton, EventTriggerType.Select, (data) =>
            {
                hostButton.ButtonText.fontSize = 14;
                hostButton.ButtonText.color = ButtonTextColor;
                main.SetActive(false);
                host.SetActive(true);
            });
            
            var connectButton = UIFactory.CreateButton(main, "connect", "Connect", BackgroundColor);
            UIFactory.SetLayoutElement(connectButton.GameObject, minWidth: 80, minHeight: 20);
            connectButton.ButtonText.color = ButtonTextColor;
            connectButton.GameObject.GetComponent<Image>().enabled = false;
            connectButton.GameObject.AddComponent<EventTrigger>();
            AddButtonEvent(connectButton, EventTriggerType.PointerExit, (data) =>
            {
                connectButton.ButtonText.fontSize = 14;
                connectButton.ButtonText.color = ButtonTextColor;
            });
            AddButtonEvent(connectButton, EventTriggerType.PointerEnter, (data) =>
            {
                connectButton.ButtonText.fontSize = 16;
                connectButton.ButtonText.color = ButtonHoverTextColor;
            });
            AddButtonEvent(connectButton, EventTriggerType.Select, (data) =>
            {
                connectButton.ButtonText.fontSize = 14;
                connectButton.ButtonText.color = ButtonTextColor;
                main.SetActive(false);
                connect.SetActive(true);
            });

            if (Plugin.Instance.Network.Online)
            {
                var disconnectButton = UIFactory.CreateButton(main, "disconnect", "Leave", BackgroundColor);
                UIFactory.SetLayoutElement(disconnectButton.GameObject, minWidth: 80, minHeight: 20);
                disconnectButton.ButtonText.color = ButtonTextColor;
                disconnectButton.GameObject.GetComponent<Image>().enabled = false;
                disconnectButton.GameObject.AddComponent<EventTrigger>();
                AddButtonEvent(disconnectButton, EventTriggerType.PointerExit, (data) =>
                {
                    disconnectButton.ButtonText.fontSize = 14;
                    disconnectButton.ButtonText.color = ButtonTextColor;
                });
                AddButtonEvent(disconnectButton, EventTriggerType.PointerEnter, (data) =>
                {
                    disconnectButton.ButtonText.fontSize = 16;
                    disconnectButton.ButtonText.color = ButtonHoverTextColor;
                });
                AddButtonEvent(disconnectButton, EventTriggerType.Select, (data) =>
                {
                    disconnectButton.ButtonText.fontSize = 14;
                    disconnectButton.ButtonText.color = ButtonTextColor;
                    main.SetActive(false);
                    connect.SetActive(true);
                });
            }
        }

        // Host group
        {
            var port = UIFactory.CreateInputField(host, "port", "Port");
            UIFactory.SetLayoutElement(port.GameObject, minWidth: 60, minHeight: 20);
            port.Component.characterValidation = InputField.CharacterValidation.Integer;
            port.Component.characterLimit = 5;
            foreach (var component in port.GameObject.GetComponentsInChildren<Text>())
            {
                component.alignment = TextAnchor.MiddleCenter;
            }
            
            var hostButton = UIFactory.CreateButton(host, "host2", "Host", BackgroundColor);
            UIFactory.SetLayoutElement(hostButton.GameObject, minWidth: 80, minHeight: 20);
            hostButton.ButtonText.color = ButtonTextColor;
            hostButton.GameObject.GetComponent<Image>().enabled = false;
            hostButton.GameObject.AddComponent<EventTrigger>();
            AddButtonEvent(hostButton, EventTriggerType.PointerExit, (data) =>
            {
                hostButton.ButtonText.fontSize = 14;
                hostButton.ButtonText.color = ButtonTextColor;
            });
            AddButtonEvent(hostButton, EventTriggerType.PointerEnter, (data) =>
            {
                hostButton.ButtonText.fontSize = 16;
                hostButton.ButtonText.color = ButtonHoverTextColor;
            });
            AddButtonEvent(hostButton, EventTriggerType.Select, (data) =>
            {
                hostButton.ButtonText.fontSize = 14;
                hostButton.ButtonText.color = ButtonTextColor;
                main.SetActive(true);
                host.SetActive(false);
                UIManager.CloseNetworkPanel();
                Plugin.Instance.Network.Host(int.Parse(port.Component.text));
            });
            
            var backButton = UIFactory.CreateButton(host, "back", "Back", BackgroundColor);
            UIFactory.SetLayoutElement(backButton.GameObject, minWidth: 80, minHeight: 20);
            backButton.ButtonText.color = ButtonTextColor;
            backButton.GameObject.GetComponent<Image>().enabled = false;
            backButton.GameObject.AddComponent<EventTrigger>();
            AddButtonEvent(backButton, EventTriggerType.PointerExit, (data) =>
            {
                backButton.ButtonText.fontSize = 14;
                backButton.ButtonText.color = ButtonTextColor;
            });
            AddButtonEvent(backButton, EventTriggerType.PointerEnter, (data) =>
            {
                backButton.ButtonText.fontSize = 16;
                backButton.ButtonText.color = ButtonHoverTextColor;
            });
            AddButtonEvent(backButton, EventTriggerType.Select, (data) =>
            {
                backButton.ButtonText.fontSize = 14;
                backButton.ButtonText.color = ButtonTextColor;
                main.SetActive(true);
                host.SetActive(false);
            });
        }

        // Connect group
        {
            var ip = UIFactory.CreateInputField(connect, "ip", "Ip Address");
            UIFactory.SetLayoutElement(ip.GameObject, minWidth: 120, minHeight: 20);
            foreach (var component in ip.GameObject.GetComponentsInChildren<Text>())
            {
                component.alignment = TextAnchor.MiddleCenter;
            }
            
            var port = UIFactory.CreateInputField(connect, "port", "Port");
            UIFactory.SetLayoutElement(port.GameObject, minWidth: 60, minHeight: 20);
            port.Component.characterValidation = InputField.CharacterValidation.Integer;
            port.Component.characterLimit = 5;
            foreach (var component in port.GameObject.GetComponentsInChildren<Text>())
            {
                component.alignment = TextAnchor.MiddleCenter;
            }
            
            var connectButton = UIFactory.CreateButton(connect, "connect", "Connect", BackgroundColor);
            UIFactory.SetLayoutElement(connectButton.GameObject, minWidth: 80, minHeight: 20);
            connectButton.ButtonText.color = ButtonTextColor;
            connectButton.GameObject.GetComponent<Image>().enabled = false;
            connectButton.GameObject.AddComponent<EventTrigger>();
            AddButtonEvent(connectButton, EventTriggerType.PointerExit, (data) =>
            {
                connectButton.ButtonText.fontSize = 14;
                connectButton.ButtonText.color = ButtonTextColor;
            });
            AddButtonEvent(connectButton, EventTriggerType.PointerEnter, (data) =>
            {
                connectButton.ButtonText.fontSize = 16;
                connectButton.ButtonText.color = ButtonHoverTextColor;
            });
            AddButtonEvent(connectButton, EventTriggerType.Select, (data) =>
            {
                connectButton.ButtonText.fontSize = 14;
                connectButton.ButtonText.color = ButtonTextColor;
                main.SetActive(true);
                connect.SetActive(false);
                UIManager.CloseNetworkPanel();
                Plugin.Instance.Network.Connect(ip.Component.text, int.Parse(port.Component.text), new ApprovalPacket());
            });
            
            var backButton = UIFactory.CreateButton(connect, "back", "Back", BackgroundColor);
            UIFactory.SetLayoutElement(backButton.GameObject, minWidth: 80, minHeight: 20);
            backButton.ButtonText.color = ButtonTextColor;
            backButton.GameObject.GetComponent<Image>().enabled = false;
            backButton.GameObject.AddComponent<EventTrigger>();
            AddButtonEvent(backButton, EventTriggerType.PointerExit, (data) =>
            {
                backButton.ButtonText.fontSize = 14;
                backButton.ButtonText.color = ButtonTextColor;
            });
            AddButtonEvent(backButton, EventTriggerType.PointerEnter, (data) =>
            {
                backButton.ButtonText.fontSize = 16;
                backButton.ButtonText.color = ButtonHoverTextColor;
            });
            AddButtonEvent(backButton, EventTriggerType.Select, (data) =>
            {
                backButton.ButtonText.fontSize = 14;
                backButton.ButtonText.color = ButtonTextColor;
                main.SetActive(true);
                connect.SetActive(false);
            });
        }

        panel.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(uiRoot.GetComponent<RectTransform>());
    }

    public override void Update()
    {
        
    }
}