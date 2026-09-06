using UnityEngine;
using UnityEngine.UI;

/// <summary>运行时 UI 小工具：字体与常用控件创建。</summary>
public static class BattleUi
{
    private static Font m_font;

    public static Font GetFont()
    {
        if (m_font == null)
        {
            m_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        if (m_font == null)
        {
            m_font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        return m_font;
    }

    public static Text AddText(Transform parent, string content, int size, Color color)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var text = go.AddComponent<Text>();
        text.font = GetFont();
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.text = content;
        return text;
    }

    public static Image AddImage(Transform parent, Color color)
    {
        var go = new GameObject("Image", typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    /// <summary>创建锚定在父节点指定位置的子 RectTransform。</summary>
    public static RectTransform AddRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pivot, Vector2 size, Vector2 anchoredPosition)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return rect;
    }
}
