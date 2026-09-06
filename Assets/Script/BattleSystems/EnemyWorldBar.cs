using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 单个敌人的世界血条：钉在头顶，处理屏幕外隐藏、
/// 距离缩放、掉血幽灵条与死亡淡出。
/// </summary>
public class EnemyWorldBar : MonoBehaviour
{
    protected Enemy m_enemy;
    protected RectTransform m_rect;
    protected CanvasGroup m_group;
    protected Image m_fill;
    protected Image m_ghost;
    protected Image m_daze;
    protected Camera m_cam;
    protected float m_baseWidth = 200f;
    protected float m_headOffset = 0.35f;
    protected float m_deathFadeTime = 0.8f;
    protected float m_ghostAmount = 1f;
    protected float m_deathTimer = -1f;

    public virtual void Setup(Enemy enemy, Image fill, Image ghost, Image daze,
        float baseWidth, float headOffset, float deathFadeTime)
    {
        m_enemy = enemy;
        m_fill = fill;
        m_ghost = ghost;
        m_daze = daze;
        m_baseWidth = baseWidth;
        m_headOffset = headOffset;
        m_deathFadeTime = Mathf.Max(0.1f, deathFadeTime);
        m_rect = (RectTransform)transform;
        m_group = GetComponent<CanvasGroup>();
        m_cam = Camera.main;
        m_ghostAmount = GetHpFraction();
        if (m_group != null)
        {
            // 首次定位前隐藏，避免首帧出现在画布原点
            m_group.alpha = 0f;
        }
    }

    protected virtual void LateUpdate()
    {
        if (m_enemy == null)
        {
            Destroy(gameObject);
            return;
        }

        if (m_enemy.isDead)
        {
            TickDeathFade();
            return;
        }

        if (m_cam == null)
        {
            m_cam = Camera.main;
        }
        if (m_cam == null || m_fill == null)
        {
            return;
        }

        Vector3 head = m_enemy.GetHeadPosition(m_headOffset);
        Vector3 view = m_cam.WorldToViewportPoint(head);

        bool onScreen = view.z > 0.05f
            && view.x > -0.15f && view.x < 1.15f
            && view.y > -0.15f && view.y < 1.15f;

        if (m_group != null)
        {
            m_group.alpha = onScreen ? 1f : 0f;
        }
        if (!onScreen)
        {
            return;
        }

        Vector3 screen = m_cam.WorldToScreenPoint(head);
        if (m_rect != null)
        {
            m_rect.position = screen;

            float distance = Vector3.Distance(m_cam.transform.position, head);
            float scale = Mathf.Clamp(2.6f / Mathf.Max(0.5f, distance), 0.6f, 2.1f);
            m_rect.localScale = Vector3.one * scale;
        }

        float current = GetHpFraction();
        if (m_ghostAmount > current)
        {
            m_ghostAmount = Mathf.MoveTowards(m_ghostAmount, current, Time.deltaTime * 0.8f);
        }
        else
        {
            m_ghostAmount = current;
        }

        SetFill(m_fill, current);
        SetFill(m_ghost, m_ghostAmount);
        if (m_daze != null)
        {
            float maxDaze = m_enemy.maxDaze;
            float daze = maxDaze > 0f ? Mathf.Clamp01(m_enemy.currentDaze / maxDaze) : 0f;
            SetFill(m_daze, daze);
        }
    }

    protected virtual float GetHpFraction()
    {
        if (m_enemy == null)
        {
            return 0f;
        }
        float max = Mathf.Max(0f, m_enemy.maxHp);
        return max > 0f ? Mathf.Clamp01(m_enemy.currentHp / max) : 0f;
    }

    protected virtual void SetFill(Image image, float amount)
    {
        if (image == null)
        {
            return;
        }
        var rect = (RectTransform)image.transform;
        rect.sizeDelta = new Vector2(m_baseWidth * Mathf.Clamp01(amount), rect.sizeDelta.y);
    }

    protected virtual void TickDeathFade()
    {
        if (m_deathTimer < 0f)
        {
            m_deathTimer = 0f;
        }
        m_deathTimer += Time.deltaTime;

        if (m_group != null)
        {
            m_group.alpha = Mathf.Clamp01(1f - m_deathTimer / m_deathFadeTime);
        }
        if (m_deathTimer >= m_deathFadeTime)
        {
            Destroy(gameObject);
        }
    }
}
