using UnityEngine;

namespace ThronefallMP.UI;

public abstract class BaseUI : MonoBehaviour
{
    public Canvas Canvas { get; set; }
    public GameObject PanelRoot { get; set; }

    public bool Enabled
    {
        get => PanelRoot.activeSelf;
        set => PanelRoot.SetActive(value);
    }
    
    public abstract string Name { get; }
    public abstract void ConstructPanelContent();

    public static T Create<T>(GameObject canvas, GameObject container) where T : BaseUI
    {
        var panelObject = new GameObject("Temp", typeof(RectTransform));
        panelObject.transform.SetParent(container.transform, false);
        var panel = panelObject.AddComponent<T>();
        panel.Canvas = canvas.GetComponent<Canvas>();
        panel.PanelRoot = panelObject;
        panel.PanelRoot.name = panel.Name;
        var rectTransform = panelObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.offsetMin = new Vector2(0, 0);
        rectTransform.offsetMax = new Vector2(0, 0);
        panel.ConstructPanelContent();
        panel.Enabled = false;
        return panel;
    }
}