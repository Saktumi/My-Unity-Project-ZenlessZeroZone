using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 世界头顶血条层：为每个敌人创建跟随血条，
/// 支持距离缩放、屏幕外隐藏、掉血幽灵条与失衡条。
/// </summary>
public class WorldBarLayer : MonoBehaviour
{
    [Header("外观")]
    public float barWidth = 240f;
    public float barHeight = 24f;
    public Color fillColor = new Color(0.35f, 0.92f, 0.55f, 1f);
    public Color ghostColor = new Color(1f, 0.35f, 0.3f, 0.85f);
    public Color dazeColor = new Color(1f, 0.82f, 0.3f, 1f);
    [Tooltip("是否显示失衡（破防）条")]
    public bool showDazeBar = true;
    [Tooltip("血量条与头顶的垂直偏移（米）")]
    public float headOffset = 0.35f;
    [Tooltip("死亡后血条保留秒数（淡出）")]
    public float deathFadeTime = 0.8f;

    protected Canvas m_canvas;
    protected readonly List<EnemyWorldBar> m_bars = new List<EnemyWorldBar>();

    public virtual void BuildAll(System.Collections.Generic.IEnumerable<Enemy> enemies)
    {
        if (enemies == null)
        {
            return;
        }
        EnsureCanvas();
        if (m_canvas == null)
        {
            return;
        }

        int index = 0;
        foreach (Enemy enemy in enemies)
        {
            if (enemy == null)
            {
                continue;
            }
            CreateBar(enemy, index);
            index++;
        }
    }

    public virtual void Clear()
    {
        for (int i = 0; i < m_bars.Count; i++)
        {
            if (m_bars[i] != null)
            {
                Destroy(m_bars[i].gameObject);
            }
        }
        m_bars.Clear();
    }

    protected virtual void EnsureCanvas()
    {
        if (m_canvas != null)
        {
            return;
        }

        var go = new GameObject("WorldBarsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(transform, false);
        go.layer = 5;

        m_canvas = go.GetComponent<Canvas>();
        m_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // 放在主 UI Canvas 之下：血条贴着角色但不盖住暂停遮罩/顶部横幅
        m_canvas.sortingOrder = -1;
        m_canvas.pixelPerfect = true;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        scaler.referencePixelsPerUnit = 100f;
    }

    protected virtual void CreateBar(Enemy enemy, int index)
    {
        var rootRect = BattleUi.AddRect(m_canvas.transform, "EnemyBar_" + enemy.name + "_" + index,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(barWidth, barHeight), Vector2.zero);

        var group = rootRect.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = false;
        group.blocksRaycasts = false;

        // 底板
        var bg = BattleUi.AddImage(rootRect, new Color(0.04f, 0.05f, 0.08f, 0.75f));
        bg.transform.SetAsFirstSibling();

        // HP 行：幽灵条在下，当前血条在上
        float hpY = showDazeBar ? 4f : 0f;
        float hpHeight = showDazeBar ? 12f : barHeight - 4f;
        float hpWidth = barWidth - 6f;

        var ghostRect = AddFillRow(rootRect, hpY, hpHeight, ghostColor, out var ghost);
        ghostRect.name = "Ghost";
        var fillRect = AddFillRow(rootRect, hpY, hpHeight, fillColor, out var fill);
        fillRect.name = "Fill";

        Image daze = null;
        if (showDazeBar)
        {
            float dazeY = -6f;
            AddFillRow(rootRect, dazeY, 5f, new Color(0.08f, 0.07f, 0.03f, 0.8f), out var dazeBg);
            dazeBg.name = "DazeBg";
            var dazeRect = AddFillRow(rootRect, dazeY, 5f, dazeColor, out daze);
            dazeRect.name = "Daze";
        }

        var bar = rootRect.gameObject.AddComponent<EnemyWorldBar>();
        bar.Setup(enemy, fill, ghost, daze, hpWidth, headOffset, deathFadeTime);
        m_bars.Add(bar);
    }

    protected virtual RectTransform AddFillRow(RectTransform parent, float y, float height, Color color, out Image image)
    {
        var rect = BattleUi.AddRect(parent, "FillRow", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(barWidth - 6f, Mathf.Max(2f, height - 2f)), new Vector2(3f, y));
        image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }
}
