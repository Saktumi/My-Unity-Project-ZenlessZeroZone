using UnityEngine;

public class Enemy_StunState : EnemyState
{
    private const float k_CrossFadeTime = 0.1f;
    private const float k_MinAnimTime = 0.3f;

    private string m_stunAnimName;

    protected override void OnEnter(Enemy enemy)
    {
        var stats = enemy.stats != null ? enemy.stats.current : null;
        m_stunAnimName = stats != null ? stats.stunAnimName : null;

        if (!string.IsNullOrEmpty(m_stunAnimName) && enemy.m_animator != null)
        {
            enemy.m_animator.CrossFadeInFixedTime(m_stunAnimName, k_CrossFadeTime);
        }
    }

    protected override void OnExit(Enemy enemy)
    {
        m_stunAnimName = null;
    }

    protected override void OnStep(Enemy enemy)
    {
        var stats = enemy.stats != null ? enemy.stats.current : null;
        bool animOk = !string.IsNullOrEmpty(m_stunAnimName) && IsStunAnimationActive(enemy);

        // 按配置时长保持眩晕以承载 QTE 窗口；动画异常时只停留最短时间
        float holdTime = animOk && stats != null ? Mathf.Max(k_MinAnimTime, stats.stunHoldTime) : k_MinAnimTime;
        if (timeSinceEntered >= holdTime)
        {
            ExitToMoveOrIdle(enemy);
        }
    }

    private void ExitToMoveOrIdle(Enemy enemy)
    {
        bool hasDirection = enemy.GetMovementDirection().sqrMagnitude > 0.01f;
        if (hasDirection)
        {
            enemy.states.Change<Enemy_MoveState>();
        }
        else
        {
            enemy.states.Change<Enemy_IdleState>();
        }
    }

    private bool IsStunAnimationActive(Enemy enemy)
    {
        var animator = enemy.m_animator;
        if (animator == null || string.IsNullOrEmpty(m_stunAnimName))
        {
            return false;
        }

        if (animator.IsInTransition(0))
        {
            return true;
        }

        return animator.GetCurrentAnimatorStateInfo(0).IsName(m_stunAnimName);
    }

    protected override void OnContact(Enemy enemy, Collider other) { }
}
