using UnityEngine;

/// <summary>
/// 玩家受击状态：按攻击来源方向选择击飞动画，可按闪避随时取消硬直。
/// </summary>
public class Player_HitState : PlayerState
{
    private const float k_CrossFadeTime = 0.08f;
    private const float k_MinHoldTime = 0.2f;
    private const float k_FinishedTime = 0.98f;

    private string m_currentAnimName;
    private int m_animHash;
    private bool m_missingAnimation;

    protected override void OnEnter(Player player)
    {
        m_missingAnimation = false;
        m_currentAnimName = null;
        m_animHash = 0;

        var hit = player.ConsumeHitRequest();
        var stats = player.stats != null ? player.stats.current : null;
        if (hit == null || stats == null || player.m_animator == null)
        {
            m_missingAnimation = true;
            return;
        }

        m_currentAnimName = IsAttackFromFront(player, hit.attackerPosition)
            ? stats.hitFlyFrontName
            : stats.hitFlyBackName;

        if (string.IsNullOrEmpty(m_currentAnimName))
        {
            m_missingAnimation = true;
            return;
        }

        // Animator 中无对应状态时标记缺失，避免 CrossFade 持续报错
        if (!player.m_animator.HasState(0, Animator.StringToHash(m_currentAnimName)))
        {
            m_missingAnimation = true;
            return;
        }

        m_animHash = Animator.StringToHash(m_currentAnimName);
        player.m_animator.CrossFadeInFixedTime(m_currentAnimName, k_CrossFadeTime);
    }

    protected override void OnExit(Player player)
    {
        m_currentAnimName = null;
        m_animHash = 0;
        m_missingAnimation = false;
    }

    protected override void OnStep(Player player)
    {
        // 闪避可随时取消硬直
        if (player.inputs != null && player.inputs.IsEvadePressed())
        {
            EvadeCancel(player);
            return;
        }

        if (m_missingAnimation)
        {
            if (timeSinceEntered < k_MinHoldTime)
            {
                return;
            }

            ExitToMoveOrIdle(player);
            return;
        }

        var animator = player.m_animator;
        var stats = player.stats != null ? player.stats.current : null;
        if (animator == null || stats == null || m_animHash == 0)
        {
            ExitToMoveOrIdle(player);
            return;
        }

        // 过渡期间等待受击动画成为当前状态
        if (animator.IsInTransition(0))
        {
            return;
        }

        // 至少展示一小段，避免状态一闪而过
        if (timeSinceEntered < k_MinHoldTime)
        {
            return;
        }

        var info = animator.GetCurrentAnimatorStateInfo(0);
        // 用短名哈希比对，兼容子状态机内的动画状态
        if (info.shortNameHash != m_animHash)
        {
            ExitToMoveOrIdle(player);
            return;
        }

        if (info.normalizedTime >= k_FinishedTime)
        {
            ExitToMoveOrIdle(player);
        }
    }

    /// <summary>闪避取消受击硬直：若处于一段闪避后的衔接窗口，按第二段闪避规则触发冷却。</summary>
    private void EvadeCancel(Player player)
    {
        if (player.CanChainEvade)
        {
            player.StartEvadeCooldown();
        }

        player.states.Change<Player_EvadeState>();
    }

    /// <summary>攻击者是否位于玩家正前方（用于选择 Front / Back 版本动画）。</summary>
    private static bool IsAttackFromFront(Player player, Vector3 attackerPosition)
    {
        var forward = player.transform.forward;
        forward.y = 0f;

        var toAttacker = attackerPosition - player.transform.position;
        toAttacker.y = 0f;

        return toAttacker.sqrMagnitude < 0.0001f
            || (forward.sqrMagnitude > 0.0001f
                && Vector3.Dot(forward.normalized, toAttacker.normalized) >= 0f);
    }

    private void ExitToMoveOrIdle(Player player)
    {
        bool hasInput = player.inputs != null && player.inputs.GetMovementDirection().sqrMagnitude > 0.01f;
        if (hasInput)
        {
            player.states.Change<Player_MoveState>();
        }
        else
        {
            player.states.Change<Player_IdleState>();
        }
    }

    protected override void OnContact(Player player, Collider other)
    {
    }
}
