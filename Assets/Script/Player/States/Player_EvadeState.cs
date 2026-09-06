using UnityEngine;

public class Player_EvadeState : PlayerState
{
    private const string k_EvadeFrontPath = "Base Layer.Move.Evade.Evade_Front";
    private const string k_EvadeBackPath = "Base Layer.Move.Evade.Evade_Back";

    private bool m_isSecondSegment;

    protected override void OnContact(Player player, Collider other)
    {
        
    }

    protected override void OnEnter(Player player)
    {
        // 闪避打断普攻时保留连击进度
        if (player.states.last is Player_NormalAttackState attackState)
        {
            attackState.PreserveCombo();
        }

        if (player.m_animator == null)
        {
            return;
        }

        bool hasInput = player.inputs != null && player.inputs.GetMovementDirection().sqrMagnitude > 0.01f;
        string evadePath = hasInput ? k_EvadeFrontPath : k_EvadeBackPath;

        m_isSecondSegment = player.CanChainEvade;

        player.m_animator.CrossFadeInFixedTime(evadePath, 0.1f);
    }

    protected override void OnExit(Player player)
    {
        // 闪避后若还在移动，强制进入跑步状态
        if (player.inputs != null && player.inputs.GetMovementDirection().sqrMagnitude > 0.01f)
        {
            player.inputs.SetRunning(true);
            if (player.p_animator != null)
            {
                player.p_animator.isRun = true;
            }
        }

        // 闪避结束开启 0.5 秒冲刺攻击窗口
        player.OpenRushAttackWindow();

        if (!m_isSecondSegment)
        {
            player.OpenEvadeChainWindow();
        }
    }

    protected override void OnStep(Player player)
    {
        if (!IsEvadeAnimationActive(player) || IsEvadeAnimationFinished(player))
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
    }

    private static bool IsEvadeAnimationActive(Player player)
    {
        var animator = player.m_animator;
        if (animator == null)
        {
            return false;
        }

        if (animator.IsInTransition(0))
        {
            return true;
        }

        var info = animator.GetCurrentAnimatorStateInfo(0);
        return info.IsName(k_EvadeFrontPath) || info.IsName(k_EvadeBackPath);
    }

    private static bool IsEvadeAnimationFinished(Player player)
    {
        var animator = player.m_animator;
        if (animator == null)
        {
            return false;
        }

        var info = animator.GetCurrentAnimatorStateInfo(0);
        return (info.IsName(k_EvadeFrontPath) || info.IsName(k_EvadeBackPath)) && info.normalizedTime >= 0.98f;
    }
}
