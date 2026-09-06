using UnityEngine;

/// <summary>
/// 敌人索敌：周期性寻找玩家，超出探测半径后丢失目标；
/// Inspector 中手动指定的目标不会被自动清除。
/// </summary>
public class EnemyTargetDetector
{
    private readonly Enemy m_enemy;

    private Player m_cachedPlayer;
    private float m_nextScanTime;
    private bool m_targetAcquiredByDetection;

    public EnemyTargetDetector(Enemy enemy)
    {
        m_enemy = enemy;
    }

    /// <summary>每帧检测：无目标时扫描，有目标时检查是否脱离。</summary>
    public void Tick()
    {
        var stats = m_enemy.stats != null ? m_enemy.stats.current : null;
        if (stats == null)
        {
            return;
        }

        if (m_enemy.target == null)
        {
            // 优先锁定当前受控成员，队伍切换后立即换目标
            Player preferred = CharacterSwitcher.instance != null ? CharacterSwitcher.instance.activePlayer : null;
            if (preferred != null && preferred.gameObject.activeInHierarchy)
            {
                m_cachedPlayer = preferred;
            }
            else if (Time.time >= m_nextScanTime)
            {
                // 无受控成员信息时按注册表找最近玩家
                m_cachedPlayer = PlayerRegistry.FindNearest(m_enemy.transform.position, stats.detectRadius);
                m_nextScanTime = Time.time + 0.5f;
            }

            if (m_cachedPlayer != null
                && m_cachedPlayer.gameObject.activeInHierarchy
                && HorizontalDistance(m_cachedPlayer.transform) <= stats.detectRadius)
            {
                m_enemy.target = m_cachedPlayer.transform;
                m_targetAcquiredByDetection = true;
            }
        }
        else if (m_targetAcquiredByDetection)
        {
            // 目标离场或已不是当前受控成员则清空重新扫描
            if (m_enemy.target == null || IsInvalidTarget(m_enemy.target))
            {
                m_enemy.target = null;
                m_targetAcquiredByDetection = false;
                m_cachedPlayer = null;
            }
            else if (m_enemy.DistanceToTarget > stats.detectRadius)
            {
                m_enemy.target = null;
                m_targetAcquiredByDetection = false;
            }
        }
    }

    private static bool IsInvalidTarget(Transform target)
    {
        if (target == null)
        {
            return true;
        }
        var player = target.GetComponentInParent<Player>();
        if (player == null)
        {
            return false;
        }
        if (!player.gameObject.activeInHierarchy)
        {
            return true;
        }
        // 队伍系统存在时只追击当前受控成员
        var team = CharacterSwitcher.instance;
        return team != null && team.activePlayer != null && team.activePlayer != player;
    }

    private float HorizontalDistance(Transform other)
    {
        if (other == null)
        {
            return float.PositiveInfinity;
        }

        var offset = other.position - m_enemy.transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }
}
