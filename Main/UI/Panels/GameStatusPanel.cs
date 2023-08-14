using Steamworks;
using ThronefallMP.Patches;
using ThronefallMP.UI.Controls;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib.UI;
using Debug = System.Diagnostics.Debug;

namespace ThronefallMP.UI.Panels;

public class GameStatusPanel : BaseUI
{
    public static readonly Color BackgroundColor = new(0.11f, 0.11f, 0.11f, 0.95f);
    
    public override string Name => "Game Status Panel";
    
    public override void ConstructPanelContent()
    {
        var container = UIFactory.CreateUIObject("container", PanelRoot);
        {
            var rectTransform = container.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(0.35f, 1);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(
                container,
                true,
                false,
                true,
                true,
                0,
                10,
                10,
                10,
                10,
                TextAnchor.UpperLeft
            );
        }
        
        var background = UIFactory.CreateUIObject("background", container);
        {
            var image = background.AddComponent<Image>();
            image.type = Image.Type.Sliced;
            image.color = BackgroundColor;
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(
                background,
                true,
                false,
                true,
                true,
                5,
                15,
                15,
                5,
                5,
                TextAnchor.UpperLeft
            );
        }

        UIHelper.CreateText(background, "test", "Testing");
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(background.GetComponent<RectTransform>());
    }

    public GameObject CreatePlayerLine()
    {
        return null;
    }
}