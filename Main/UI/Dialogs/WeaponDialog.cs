using Steamworks;
using ThronefallMP.Network;
using ThronefallMP.Network.Packets.Game;
using ThronefallMP.Patches;
using ThronefallMP.UI.Controls;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UniverseLib.UI;

namespace ThronefallMP.UI.Dialogs;

public class WeaponDialog : BaseUI
{
    public override string Name => "Weapon Dialog";

    public string ButtonText
    {
        get => _button.Text.text;
        set => _button.Text.text = value;
    }

    private delegate void WeaponChanged();

    private event WeaponChanged OnWeaponChanged;
    private TextMeshProUGUI _title;
    private GameObject _background;
    private ButtonControl _button;
    private Equipment _selectedEquipment;
    
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
            // TODO: Change this to fit content automatically.
            var rectTransform = panelBorders.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.35f, 0.35f);
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

        var titleContainer = UIFactory.CreateUIObject("titleContainer", panel);
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
        
        _title = UIHelper.CreateText(titleContainer, "title", "Weapon Selection");
        _title.fontSize = 36;
        _title.alignment = TextAlignmentOptions.Center;

        var weapons = UIFactory.CreateUIObject("weapons", panel);
        {
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(
                weapons,
                false,
                false,
                false,
                false,
                20,
                0,
                0,
                0,
                0,
                TextAnchor.MiddleCenter
            );
        }

        AddWeaponIcon(weapons, Equipment.LongBow);
        AddWeaponIcon(weapons, Equipment.LightSpear);
        AddWeaponIcon(weapons, Equipment.HeavySword);
        
        _selectedEquipment = Equipment.LongBow;
        OnWeaponChanged?.Invoke();

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
        
        _button = UIHelper.CreateButton(buttons, "button", "Confirm");
        UIFactory.SetLayoutElement(_button.gameObject, minWidth: 100);
        _button.OnClick += () =>
        {
            var response = new WeaponResponsePacket
            {
                Weapon = _selectedEquipment
            };

            if (Plugin.Instance.Network.Server)
            {
                var id = new SteamNetworkingIdentity();
                id.SetSteamID(Plugin.Instance.PlayerManager.LocalPlayer.SteamID);
                PacketHandler.HandlePacket(id, response);
            }
            else
            {
                Plugin.Instance.Network.Send(response);
            }
            
            Destroy(gameObject);
        };
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelBorders.GetComponent<RectTransform>());
    }

    private void AddWeaponIcon(GameObject parent, Equipment equipment)
    {
        var normal = new Color(0.153f, 0.216f, 0.294f, 1);
        var selected = new Color(0.106f, 0.322f, 0.584f, 1);
        var outlineNormal = new Color(0.784f, 0.651f, 0.455f, 1);
        var outlineSelected = new Color(1, 0.906f, 0.769f, 1);
        
        var frame = UIFactory.CreateUIObject($"weapon_{equipment}", parent);
        {
            var rectTransform = frame.GetComponent<RectTransform>();
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(32, 32);

            var image = frame.AddComponent<MPUIKIT.MPImage>();
            image.FalloffDistance = 0.5f;
            image.OutlineWidth = 4;
            
            frame.AddComponent<EventTrigger>();
            UIHelper.AddEvent(frame, EventTriggerType.PointerClick, (_) =>
            {
                _selectedEquipment = equipment;
                OnWeaponChanged?.Invoke();
            });
            UIHelper.AddEvent(frame, EventTriggerType.Submit, (_) =>
            {
                _selectedEquipment = equipment;
                OnWeaponChanged?.Invoke();
            });
            UIHelper.AddEvent(frame, EventTriggerType.PointerEnter, (_) =>
            {
                image.OutlineColor = outlineSelected;
            });
            UIHelper.AddEvent(frame, EventTriggerType.PointerExit, (_) =>
            {
                image.OutlineColor = _selectedEquipment == equipment
                    ? outlineSelected
                    : outlineNormal;
            });
        }
        
        var icon = UIFactory.CreateUIObject($"weapon_{equipment}", frame);
        {
            var rectTransform = icon.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            var image = icon.AddComponent<Image>();
            image.type = Image.Type.Filled;
            image.sprite = Equip.Convert(equipment).icon;
        }
        
        OnWeaponChanged += () =>
        {
            var image = frame.GetComponent<MPUIKIT.MPImage>();
            image.color = _selectedEquipment == equipment
                ? selected
                : normal;
            image.OutlineColor = _selectedEquipment == equipment
                ? outlineSelected
                : outlineNormal;
        };
    }

    public void OnEnable()
    {
        ++UIFramePatch.DisableGameUIInputCount;
        if (UIFrameManager.instance != null)
        {
            UIFrameManager.instance.CloseAllFrames();
        }

        if (LocalGamestate.Instance != null)
        {
            LocalGamestate.Instance.SetPlayerFreezeState(true);
        }
    }

    public void OnDisable()
    {
        --UIFramePatch.DisableGameUIInputCount;
        if (LocalGamestate.Instance != null)
        {
            LocalGamestate.Instance.SetPlayerFreezeState(false);
        }
    }
}