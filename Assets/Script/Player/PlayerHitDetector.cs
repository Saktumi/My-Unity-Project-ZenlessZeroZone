using UnityEngine;

/// <summary>
/// 玩家命中检测：在攻击动画的命中窗口内检查前方敌人，
/// 命中后结算伤害并触发震屏 / 顿帧表现。
/// </summary>
public class PlayerHitDetector
{
    private readonly Player m_player;
    private bool m_triggered;

    public PlayerHitDetector(Player player)
    {
        m_player = player;
    }

    /// <summary>每段攻击起手时重置命中标记。</summary>
    public void ResetTrigger()
    {
        m_triggered = false;
    }

    /// <summary>命中窗口内结算伤害，causesStun 决定敌人进入眩晕或普通受击。</summary>
    public void Tick(float normalizedTime, float damage, bool causesStun)
    {
        if (m_triggered || m_player == null)
        {
            return;
        }

        var stats = m_player.stats != null ? m_player.stats.current : null;
        if (stats == null || stats.hitRange <= 0f)
        {
            return;
        }

        // 动画回到窗口起点（新一段攻击）时重新武装
        if (normalizedTime < stats.hitWindowStart)
        {
            m_triggered = false;
            return;
        }

        if (normalizedTime > stats.hitWindowEnd)
        {
            return;
        }

        var enemy = FindEnemyInFront(stats.hitRange, stats.hitAngle);
        if (enemy == null)
        {
            return;
        }

        m_triggered = true;

        float finalDamage = m_player.RollDamage(damage, out bool isCrit);
        if (finalDamage <= 0f)
        {
            return;
        }

        enemy.TakeDamage(finalDamage, causesStun);
        var damagePool = FloatingDamagePool.Ensure();
        if (damagePool != null)
        {
            damagePool.Spawn(enemy.GetHeadPosition(0.35f), finalDamage, isCrit, false);
        }
        m_player.PlayHitImpulse(stats.hitShakeForce);

        var feel = HitFeelManager.instance;
        if (feel != null)
        {
            feel.HitStop(stats.hitStopTime);
        }
    }

    private Enemy FindEnemyInFront(float range, float maxAngle)
    {
        if (m_player == null)
        {
            return null;
        }

        return EnemyRegistry.FindNearestInFront(
            m_player.transform.position, m_player.transform.forward, range, maxAngle);
    }
}
