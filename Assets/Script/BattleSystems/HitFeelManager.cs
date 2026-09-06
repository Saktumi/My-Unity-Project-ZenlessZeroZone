using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 打击感管理：顿帧冻结角色动画，慢动作覆盖 timeScale，
/// 恢复时按暂停状态还原。
/// </summary>
public class HitFeelManager : MonoBehaviour
{
    public static HitFeelManager instance { get; protected set; }

    public float currentTimeScale => Time.timeScale;
    public bool HasOverride => m_overrideUntil > Time.unscaledTime;
    public bool IsHitStopping => m_hitStopRoutine != null;

    protected float m_overrideUntil = -1f;
    protected float m_overrideScale = 1f;
    protected Coroutine m_hitStopRoutine;
    protected readonly Dictionary<Animator, float> m_animatorSpeeds = new Dictionary<Animator, float>();

    protected virtual void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }
        instance = this;
    }

    protected virtual void OnDestroy()
    {
        if (m_hitStopRoutine != null)
        {
            StopCoroutine(m_hitStopRoutine);
            m_hitStopRoutine = null;
        }
        RestoreAnimators();
        if (instance == this)
        {
            instance = null;
        }
    }

    protected virtual void Update()
    {
        if (m_overrideUntil < 0f)
        {
            return;
        }
        if (Time.unscaledTime >= m_overrideUntil)
        {
            RestoreNow();
        }
    }

    /// <summary>顿帧：冻结场上角色动画 duration 秒，不修改 timeScale。</summary>
    public virtual void HitStop(float duration)
    {
        if (duration <= 0f)
        {
            return;
        }

        if (m_hitStopRoutine != null)
        {
            StopCoroutine(m_hitStopRoutine);
        }

        CaptureAndFreezeAnimators();

        m_hitStopRoutine = StartCoroutine(HitStopRoutine(duration));
    }

    /// <summary>在一段时间内覆盖 timeScale，用于 QTE 时停等慢动作。</summary>
    public virtual void SlowMotion(float scale, float duration)
    {
        if (duration <= 0f)
        {
            return;
        }
        m_overrideScale = Mathf.Clamp(scale, 0.02f, 1f);
        m_overrideUntil = Time.unscaledTime + Mathf.Max(0f, duration);
        Time.timeScale = m_overrideScale;
    }

    /// <summary>立即结束慢动作 / 顿帧并恢复。</summary>
    public virtual void RestoreNow()
    {
        m_overrideUntil = -1f;
        m_overrideScale = 1f;
        Time.timeScale = GetBaseTimeScale();

        if (m_hitStopRoutine != null)
        {
            StopCoroutine(m_hitStopRoutine);
            m_hitStopRoutine = null;
        }
        RestoreAnimators();
    }

    protected virtual IEnumerator HitStopRoutine(float duration)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0.02f, duration));
        m_hitStopRoutine = null;
        RestoreAnimators();
    }

    protected virtual void CaptureAndFreezeAnimators()
    {
        for (int i = 0; i < PlayerRegistry.Count; i++)
        {
            var player = PlayerRegistry.GetAt(i);
            FreezeAnimatorOf(player != null ? player.m_animator : null);
        }

        for (int i = 0; i < EnemyRegistry.Count; i++)
        {
            var enemy = EnemyRegistry.GetAt(i);
            FreezeAnimatorOf(enemy != null ? enemy.m_animator : null);
        }
    }

    protected virtual void FreezeAnimatorOf(Animator animator)
    {
        if (animator == null)
        {
            return;
        }
        if (!m_animatorSpeeds.ContainsKey(animator))
        {
            m_animatorSpeeds.Add(animator, animator.speed);
        }
        animator.speed = 0f;
    }

    protected virtual void RestoreAnimators()
    {
        if (m_animatorSpeeds.Count == 0)
        {
            return;
        }
        foreach (var pair in m_animatorSpeeds)
        {
            if (pair.Key != null)
            {
                pair.Key.speed = pair.Value;
            }
        }
        m_animatorSpeeds.Clear();
    }

    protected virtual float GetBaseTimeScale()
    {
        var pauser = LevelPauser.instance;
        if (pauser != null && pauser.paused)
        {
            return 0f;
        }
        return 1f;
    }
}
