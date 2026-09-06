using UnityEngine;

/// <summary>
/// 敌人的战斗请求与攻击冷却：管理受击 / 眩晕 / 死亡标记及随机攻击间隔。
/// </summary>
public class EnemyCombat
{
    private readonly Enemy m_enemy;

    private bool m_attackRequested;
    private bool m_hitRequested;
    private bool m_stunRequested;
    private bool m_deathRequested;

    public float attackCooldownRemaining { get; private set; }

    public EnemyCombat(Enemy enemy)
    {
        m_enemy = enemy;
    }

    /// <summary>是否有攻击请求：外部显式请求或自动攻击条件满足。</summary>
    public bool IsAttackRequested() => m_attackRequested || m_enemy.CanAutoAttack;

    /// <summary>显式请求一次攻击。</summary>
    public void RequestAttack() => m_attackRequested = true;

    /// <summary>取出并清除攻击请求。</summary>
    public bool ConsumeAttackRequest()
    {
        bool requested = m_attackRequested;
        m_attackRequested = false;
        return requested;
    }

    /// <summary>攻击冷却倒计时。</summary>
    public void TickAttackCooldown()
    {
        if (attackCooldownRemaining > 0f)
        {
            attackCooldownRemaining = Mathf.Max(0f, attackCooldownRemaining - Time.deltaTime);
        }
    }

    /// <summary>开始随机攻击冷却。</summary>
    public void StartAttackCooldown()
    {
        var stats = m_enemy.stats != null ? m_enemy.stats.current : null;
        if (stats == null)
        {
            attackCooldownRemaining = Random.Range(1f, 3f);
            return;
        }

        float min = Mathf.Max(0f, stats.attackCooldownMin);
        float max = Mathf.Max(min, stats.attackCooldownMax);
        attackCooldownRemaining = Random.Range(min, max);
    }

    /// <summary>请求受击（由外部伤害系统调用）。</summary>
    public void RequestHit() => m_hitRequested = true;

    /// <summary>请求眩晕（由外部伤害系统调用）。</summary>
    public void RequestStun() => m_stunRequested = true;

    /// <summary>请求死亡（由外部伤害系统调用）。</summary>
    public void RequestDeath() => m_deathRequested = true;

    public bool ConsumeHitRequest()
    {
        bool requested = m_hitRequested;
        m_hitRequested = false;
        return requested;
    }

    public bool ConsumeStunRequest()
    {
        bool requested = m_stunRequested;
        m_stunRequested = false;
        return requested;
    }

    public bool ConsumeDeathRequest()
    {
        bool requested = m_deathRequested;
        m_deathRequested = false;
        return requested;
    }

    /// <summary>清空遗留请求，进入死亡状态时调用。</summary>
    public void ClearRequests()
    {
        m_attackRequested = false;
        m_hitRequested = false;
        m_stunRequested = false;
        m_deathRequested = false;
    }
}
