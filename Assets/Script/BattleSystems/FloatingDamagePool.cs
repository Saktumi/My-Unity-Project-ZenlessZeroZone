using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 伤害飘字：对象池 + 屏幕空间 Overlay，
/// 首次使用时自动创建画布，无需场景摆放。
/// </summary>
public class FloatingDamagePool : MonoBehaviour
{
    public static FloatingDamagePool instance { get; protected set; }

    [Header("飘字表现")]
    [Tooltip("单个数字存活时长（秒）")]
    public float lifeTime = 0.8f;
    [Tooltip("上飘速度（世界单位/秒）")]
    public float riseSpeed = 1.4f;
    [Tooltip("普通伤害字号")]
    public float normalFontSize = 30f;
    [Tooltip("暴击字号")]
    public float critFontSize = 44f;

    protected Canvas m_canvas;
    protected readonly Stack<FloatingDamageText> m_pool = new Stack<FloatingDamageText>();

    public static FloatingDamagePool Ensure()
    {
        if (instance != null)
        {
            return instance;
        }
        var go = new GameObject("FloatingDamagePool");
        return go.AddComponent<FloatingDamagePool>();
    }

    protected virtual void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        EnsureCanvas();
    }

    protected virtual void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    /// <summary>在目标头顶生成一个飘字。</summary>
    public virtual void Spawn(Vector3 worldPosition, float amount, bool isCrit, bool damageToPlayer)
    {
        var item = Acquire();
        Color color = damageToPlayer
            ? new Color(1f, 0.36f, 0.36f, 1f)
            : isCrit
                ? new Color(1f, 0.84f, 0.30f, 1f)
                : Color.white;
        item.Show(worldPosition, Format(amount), color,
            isCrit ? critFontSize : normalFontSize, lifeTime, riseSpeed);
    }

    protected virtual string Format(float amount)
    {
        return Mathf.RoundToInt(amount).ToString();
    }

    protected virtual void EnsureCanvas()
    {
        if (m_canvas != null)
        {
            return;
        }

        var canvasGo = new GameObject("FloatingDamageCanvas",
            typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);

        m_canvas = canvasGo.GetComponent<Canvas>();
        m_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        m_canvas.sortingOrder = 500;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        scaler.referencePixelsPerUnit = 100f;
    }

    protected virtual FloatingDamageText Acquire()
    {
        FloatingDamageText item = null;
        while (item == null && m_pool.Count > 0)
        {
            item = m_pool.Pop();
        }
        if (item == null)
        {
            var go = new GameObject("DamageText", typeof(RectTransform),
                typeof(CanvasGroup), typeof(Text), typeof(FloatingDamageText));
            go.transform.SetParent(m_canvas.transform, false);
            go.layer = 5;
            item = go.GetComponent<FloatingDamageText>();
            item.Initialize();
        }
        item.gameObject.SetActive(true);
        return item;
    }

    public virtual void Release(FloatingDamageText item)
    {
        if (item == null)
        {
            return;
        }
        item.gameObject.SetActive(false);
        m_pool.Push(item);
    }
}

/// <summary>单个飘字：上飘淡出，生命周期结束后回池。</summary>
public class FloatingDamageText : MonoBehaviour
{
    protected Text m_text;
    protected RectTransform m_rect;
    protected Camera m_camera;
    protected Vector3 m_worldPosition;
    protected float m_age;
    protected float m_life;
    protected float m_riseSpeed;
    protected bool m_running;

    public void Initialize()
    {
        m_rect = (RectTransform)transform;
        m_text = GetComponent<Text>();
        var group = GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = gameObject.AddComponent<CanvasGroup>();
        }
        if (m_text != null)
        {
            m_text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (m_text.font == null)
            {
                m_text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            m_text.alignment = TextAnchor.MiddleCenter;
            m_text.horizontalOverflow = HorizontalWrapMode.Overflow;
            m_text.verticalOverflow = VerticalWrapMode.Overflow;
            m_text.raycastTarget = false;
        }
        m_running = false;
    }

    public void Show(Vector3 worldPosition, string content, Color color,
        float fontSize, float life, float riseSpeed)
    {
        m_worldPosition = worldPosition;
        m_age = 0f;
        m_life = Mathf.Max(0.1f, life);
        m_riseSpeed = riseSpeed;
        m_running = true;

        if (m_text != null)
        {
            m_text.text = content;
            m_text.color = color;
            m_text.fontSize = Mathf.RoundToInt(fontSize);
        }
        var group = GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.alpha = 1f;
        }
        m_camera = Camera.main;
        RefreshScreenPosition();
    }

    protected virtual void Update()
    {
        if (!m_running)
        {
            return;
        }

        m_age += Time.unscaledDeltaTime;
        if (m_age >= m_life)
        {
            m_running = false;
            var pool = FloatingDamagePool.instance;
            if (pool != null)
            {
                pool.Release(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
            return;
        }

        RefreshScreenPosition();
        var group = GetComponent<CanvasGroup>();
        if (group != null)
        {
            float fade = Mathf.Clamp01(1f - m_age / m_life);
            group.alpha = fade * fade;
        }
    }

    protected virtual void RefreshScreenPosition()
    {
        if (m_camera == null)
        {
            m_camera = Camera.main;
        }
        if (m_camera == null || m_rect == null)
        {
            return;
        }

        Vector3 world = m_worldPosition + Vector3.up * (m_age * m_riseSpeed);
        Vector3 screen = m_camera.WorldToScreenPoint(world);
        if (screen.z <= 0f)
        {
            return;
        }
        m_rect.position = screen;
    }
}
