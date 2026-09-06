using UnityEngine;

/// <summary>攻击吸附：起手时向范围内最近敌人转向并小幅推进。</summary>
public class PlayerAttackSnap
{
    private readonly Player m_player;

    public PlayerAttackSnap(Player player)
    {
        m_player = player;
    }

    /// <summary>攻击开始时调用。找到可吸附目标并完成转向 / 推进时返回 true。</summary>
    public bool TrySnap()
    {
        if (m_player == null)
        {
            return false;
        }

        var stats = m_player.stats != null ? m_player.stats.current : null;
        if (stats == null || stats.attackSnapRange <= 0f)
        {
            return false;
        }

        var target = FindNearestTarget(stats.attackSnapRange);
        if (target == null)
        {
            if (stats.attackSnapDebug)
            {
                GameLog.Log($"[AttackSnap] {stats.attackSnapRange} 范围内没有敌人，跳过吸附");
            }
            return false;
        }

        var offset = target.position - m_player.transform.position;
        offset.y = 0f;
        if (offset.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        var toTarget = offset.normalized;

        // 默认 180° 表示不限转身幅度
        var forward = m_player.transform.forward;
        forward.y = 0f;
        float angle = Vector3.Angle(forward.normalized, toTarget);
        if (stats.attackSnapMaxAngle < 180f && angle > stats.attackSnapMaxAngle)
        {
            if (stats.attackSnapDebug)
            {
                GameLog.Log($"[AttackSnap] 目标夹角 {angle:F1}° 超过 {stats.attackSnapMaxAngle}°，跳过吸附");
            }
            return false;
        }

        m_player.transform.rotation = Quaternion.LookRotation(toTarget, Vector3.up);

        if (stats.attackSnapPullDistance > 0f
            && m_player.controller != null
            && m_player.controller.enabled)
        {
            m_player.controller.Move(toTarget * stats.attackSnapPullDistance);
        }

        if (stats.attackSnapDebug)
        {
            GameLog.Log($"[AttackSnap] 已吸附：距离 {offset.magnitude:F2}，夹角 {angle:F1}°");
        }

        return true;
    }

    private Transform FindNearestTarget(float maxDistance)
    {
        if (m_player == null)
        {
            return null;
        }

        var enemy = EnemyRegistry.FindNearest(m_player.transform.position, maxDistance);
        return enemy != null ? enemy.transform : null;
    }
}
