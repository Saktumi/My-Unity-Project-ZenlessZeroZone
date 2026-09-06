using UnityEngine;

public class Enemy_AttackState : EnemyState
{
    private const float k_CrossFadeTime = 0.05f;
    private const float k_FinishedTime = 0.98f;

    private string m_currentAnimName;
    private Player m_playerTarget;
    private bool m_hitDealt;

    protected override void OnContact(Enemy enemy, Collider other) { }

    protected override void OnEnter(Enemy enemy)
    {
        if (enemy.m_animator == null)
        {
            return;
        }

        // 从动画名池随机选取一段攻击
        m_currentAnimName = PickRandomAttackName(enemy);
        if (!string.IsNullOrEmpty(m_currentAnimName))
        {
            enemy.m_animator.CrossFadeInFixedTime(m_currentAnimName, k_CrossFadeTime);
        }

        m_playerTarget = ResolvePlayerTarget(enemy);
        m_hitDealt = false;
    }

    protected override void OnExit(Enemy enemy)
    {
        m_currentAnimName = null;
        m_playerTarget = null;

        // 攻击结束进入随机间隔
        enemy.StartAttackCooldown();
    }

    protected override void OnStep(Enemy enemy)
    {
        // 命中结算先于退出判断，避免最后帧漏判
        TickHitWindow(enemy);

        // 动画未配置 / 未正常播放 / 已播完则退出
        if (string.IsNullOrEmpty(m_currentAnimName)
            || !IsAttackAnimationPlaying(enemy)
            || IsAttackAnimationFinished(enemy))
        {
            ExitToMoveOrIdle(enemy);
        }
    }

    /// <summary>命中窗口内对正面目标结算一次伤害。</summary>
    private void TickHitWindow(Enemy enemy)
    {
        if (m_hitDealt || m_playerTarget == null || enemy.m_animator == null)
        {
            return;
        }

        // 目标已切下场：本次攻击挥空
        if (!m_playerTarget.gameObject.activeInHierarchy)
        {
            return;
        }

        // 只有当前受控成员能被命中
        var team = CharacterSwitcher.instance;
        if (team != null && team.activePlayer != null && team.activePlayer != m_playerTarget)
        {
            return;
        }

        var stats = enemy.stats != null ? enemy.stats.current : null;
        if (stats == null || string.IsNullOrEmpty(m_currentAnimName))
        {
            return;
        }

        // 过渡期间不结算，避免读到上一段动画进度
        if (enemy.m_animator.IsInTransition(0))
        {
            return;
        }

        var info = enemy.m_animator.GetCurrentAnimatorStateInfo(0);
        if (!info.IsName(m_currentAnimName))
        {
            return;
        }

        if (info.normalizedTime < stats.attackHitWindowStart
            || info.normalizedTime > stats.attackHitWindowEnd)
        {
            return;
        }

        if (!IsTargetInHitRange(enemy, stats))
        {
            return;
        }

        m_hitDealt = true;

        // 按最大生命值比例结算，保证存在真实击倒威胁
        float minRatio = Mathf.Max(0f, stats.attackDamageMinRatio);
        float maxRatio = Mathf.Max(minRatio, stats.attackDamageMaxRatio);
        float damage = m_playerTarget.maxHp * Random.Range(minRatio, maxRatio);
        bool dealt = m_playerTarget.TakeDamage(damage, enemy.transform.position);
        if (dealt)
        {
            var damagePool = FloatingDamagePool.Ensure();
            if (damagePool != null)
            {
                damagePool.Spawn(m_playerTarget.GetHeadPosition(0.3f), damage, false, true);
            }
            m_playerTarget.PlayHitImpulse(0.55f);
            var feel = HitFeelManager.instance;
            if (feel != null)
            {
                feel.HitStop(0.05f);
            }
        }
    }

    /// <summary>目标是否处于命中距离与朝向夹角内。</summary>
    private bool IsTargetInHitRange(Enemy enemy, EnemyStats stats)
    {
        var target = m_playerTarget != null ? m_playerTarget.transform : null;
        if (target == null)
        {
            return false;
        }

        var offset = target.position - enemy.transform.position;
        offset.y = 0f;

        float range = stats.attackRange * Mathf.Max(0.1f, stats.hitRangeScale);
        if (offset.sqrMagnitude > range * range)
        {
            return false;
        }

        var forward = enemy.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        return Vector3.Angle(forward, offset.normalized) <= Mathf.Clamp(stats.hitAngle, 0f, 180f);
    }

    /// <summary>解析玩家目标：从 target 或其父级找 Player，找不到则回退到场景查找。</summary>
    private Player ResolvePlayerTarget(Enemy enemy)
    {
        if (enemy.target != null)
        {
            var player = enemy.target.GetComponent<Player>();
            if (player == null)
            {
                player = enemy.target.GetComponentInParent<Player>();
            }

            if (player != null)
            {
                return player;
            }
        }

        var fallback = UnityEngine.Object.FindFirstObjectByType<Player>();
        if (fallback == null)
        {
            Debug.LogWarning("[Enemy_AttackState] 未找到可命中的玩家：target 上没有 Player，场景中也没有 Player。");
        }
        return fallback;
    }

    private string PickRandomAttackName(Enemy enemy)
    {
        var names = GetAttackNames(enemy);
        if (names == null || names.Length == 0)
        {
            return null;
        }

        return names[Random.Range(0, names.Length)];
    }

    private string[] GetAttackNames(Enemy enemy)
    {
        var stats = enemy.stats != null ? enemy.stats.current : null;
        return stats != null ? stats.normalName : null;
    }

    private bool IsAttackAnimationPlaying(Enemy enemy)
    {
        var animator = enemy.m_animator;
        if (animator == null || string.IsNullOrEmpty(m_currentAnimName))
        {
            return false;
        }

        if (animator.IsInTransition(0))
        {
            return true;
        }

        return animator.GetCurrentAnimatorStateInfo(0).IsName(m_currentAnimName);
    }

    private bool IsAttackAnimationFinished(Enemy enemy)
    {
        var animator = enemy.m_animator;
        if (animator == null || string.IsNullOrEmpty(m_currentAnimName))
        {
            return false;
        }

        var info = animator.GetCurrentAnimatorStateInfo(0);
        return info.IsName(m_currentAnimName) && info.normalizedTime >= k_FinishedTime;
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
}
