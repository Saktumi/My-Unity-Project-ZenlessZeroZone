using UnityEngine;

public class Enemy_HitState : EnemyState
{
    private const float k_CrossFadeTime = 0.05f;
    private const float k_MinHoldTime = 0.35f;

    private string m_hitAnimName;

    protected override void OnEnter(Enemy enemy)
    {
        var stats = enemy.stats != null ? enemy.stats.current : null;
        m_hitAnimName = stats != null ? stats.hitAnimName : null;

        if (!string.IsNullOrEmpty(m_hitAnimName) && enemy.m_animator != null)
        {
            enemy.m_animator.CrossFadeInFixedTime(m_hitAnimName, k_CrossFadeTime);
        }
    }

    protected override void OnExit(Enemy enemy)
    {
        m_hitAnimName = null;
    }

    protected override void OnStep(Enemy enemy)
    {
        // 至少展示一小段受击反应
        if (timeSinceEntered < k_MinHoldTime)
        {
            return;
        }

        // 动画未配置 / 未正常播放 / 已播完则退出
        if (string.IsNullOrEmpty(m_hitAnimName)
            || !IsHitAnimationActive(enemy)
            || IsHitAnimationFinished(enemy))
        {
            ExitToMoveOrIdle(enemy);
        }
    }

    private bool IsHitAnimationActive(Enemy enemy)
    {
        var animator = enemy.m_animator;
        if (animator == null || string.IsNullOrEmpty(m_hitAnimName))
        {
            return false;
        }

        if (animator.IsInTransition(0))
        {
            return true;
        }

        return animator.GetCurrentAnimatorStateInfo(0).IsName(m_hitAnimName);
    }

    private bool IsHitAnimationFinished(Enemy enemy)
    {
        var animator = enemy.m_animator;
        if (animator == null || string.IsNullOrEmpty(m_hitAnimName))
        {
            return false;
        }

        var info = animator.GetCurrentAnimatorStateInfo(0);
        return info.IsName(m_hitAnimName) && info.normalizedTime >= 0.98f;
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

    protected override void OnContact(Enemy enemy, Collider other) { }
}
