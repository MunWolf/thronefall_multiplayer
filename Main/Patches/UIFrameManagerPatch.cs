using HarmonyLib;
using I2.Loc;
using ThronefallMP.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ThronefallMP.Patches;

public static class UIFrameManagerPatch
{
    private static RectTransform[] _buttonTransforms;
    private static VerticalLayoutGroup _menuItems;
    private static TFUITextButton _resignButton;
    private static TFUITextButton _disconnectButton;
    private static PauseUILoadoutHelper _pauseUI;
    
    public static void Apply()
    {
        On.UIFrameManager.OpenMenu += OpenMenu;
        On.UIFrameManager.ProcessFrameChange += ProcessFrameChange;
    }

    private static void OpenMenu(On.UIFrameManager.orig_OpenMenu original, UIFrameManager self)
    {
        if (self.ActiveFrame == null && !SceneTransitionManagerPatch.InLevelSelect)
        {
            if (_disconnectButton == null)
            {
                _pauseUI = GameObject.Find("UI Canvas").transform.Find("InMatch Pause Frame").GetComponent<PauseUILoadoutHelper>();
                var menu = _pauseUI.transform.Find("Title /Menu Items");
                _menuItems = menu.GetComponent<VerticalLayoutGroup>();

                _resignButton = menu.Find("Back To Menu").GetComponent<TFUITextButton>();
                
                var disconnect = Helpers.InstantiateDisabled(_resignButton.gameObject, _resignButton.gameObject.transform.parent);
                disconnect.name = "Disconnect";
                _disconnectButton = disconnect.GetComponent<TFUITextButton>();
                var textMesh = disconnect.GetComponent<TextMeshProUGUI>();
                Object.DestroyImmediate(disconnect.GetComponent<Localize>());
                
                Traverse.Create(_disconnectButton).Field<string>("originalString").Value = "Disconnect";
                textMesh.text = "Disconnect";
                textMesh.SetAllDirty();
                _disconnectButton.onApply.m_PersistentCalls.Clear();
                _disconnectButton.onApply.AddListener(() =>
                {
                    Plugin.Instance.Network.Local();
                    SceneTransitionManagerPatch.Transition("_StartMenu", SceneTransitionManagerPatch.CurrentScene);
                });
                
                _disconnectButton.topNav = _resignButton;
                _disconnectButton.botNav = _resignButton.botNav;

                var transform = _disconnectButton.GetComponent<RectTransform>();
                transform.sizeDelta = transform.sizeDelta with { y = 80 };
                _buttonTransforms = new[]
                {
                    menu.Find("Continue").GetComponent<RectTransform>(),
                    menu.Find("Settings").GetComponent<RectTransform>(),
                    _resignButton.GetComponent<RectTransform>()
                };
            }

            if (Plugin.Instance.Network.Online)
            {
                foreach (var transform in _buttonTransforms)
                {
                    transform.sizeDelta = transform.sizeDelta with { y = 80 };
                }
                
                _disconnectButton.gameObject.SetActive(true);
                _resignButton.botNav = _disconnectButton;
                _pauseUI.botmostButton = _disconnectButton;
                _menuItems.spacing = 5;
                _pauseUI.Refresh();
            }
            else
            {
                foreach (var transform in _buttonTransforms)
                {
                    transform.sizeDelta = transform.sizeDelta with { y = 100 };
                }
                
                _disconnectButton.gameObject.SetActive(false);
                _resignButton.botNav = _disconnectButton.botNav;
                _pauseUI.botmostButton = _resignButton;
                _menuItems.spacing = 10;
                _pauseUI.Refresh();
            }
        }

        original(self);
    }

    private static void ProcessFrameChange(
        On.UIFrameManager.orig_ProcessFrameChange original,
        UIFrameManager self,
        UIFrame nextframe,
        bool writeoldframetostack,
        bool keepoldframegameobjectactive,
        ThronefallUIElement firstselected)
    {
        if (nextframe != null)
        {
            nextframe.freezeTime = false;
        }
        
        original(
            self,
            nextframe,
            writeoldframetostack,
            keepoldframegameobjectactive,
            firstselected
        );
    }
}