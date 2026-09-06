using UnityEngine;

public class Player_SkillState : PlayerState
{
    // 进入本状态前由 PlayerState.TryEnterSkill 写入动作
    public enum SkillAction
    {
        None,
        Skill,
        SpecialSkill,
        AssistSkill,
        FinishSkill,
        FinishSkillPass,
    }

    private const float k_CrossFadeTime = 0.05f;
    private const float k_ExitTime = 0.85f;
    private const float k_FinishSkillExitTime = 0.98f;

    private SkillAction m_action;
    private string m_currentAnimName;

    /// <summary>当前是否处于终结技及收尾阶段。</summary>
    public bool IsFinishSkillAction =>
        m_action == SkillAction.FinishSkill || m_action == SkillAction.FinishSkillPass;

    /// <summary>当前执行的技能动作。</summary>
    public SkillAction currentAction => m_action;

    // 对应动画名是否已在属性资产中配置
    public static bool HasConfiguredAction(Player player, SkillAction action)
    {
        var stats = player != null && player.stats != null ? player.stats.current : null;
        if (stats == null)
        {
            return false;
        }

        switch (action)
        {
            case SkillAction.Skill: return !string.IsNullOrEmpty(stats.skillName);
            case SkillAction.SpecialSkill: return !string.IsNullOrEmpty(stats.specialSkillName);
            case SkillAction.AssistSkill: return !string.IsNullOrEmpty(stats.assistSkillName);
            case SkillAction.FinishSkill: return !string.IsNullOrEmpty(stats.finishSkillName);
            case SkillAction.FinishSkillPass: return !string.IsNullOrEmpty(stats.finishSkillPassName);
            default: return false;
        }
    }

    protected override void OnContact(Player player, Collider other)
    {
    }

    protected override void OnEnter(Player player)
    {
        m_action = player.ConsumeRequestedSkill();
        if (m_action == SkillAction.None)
        {
            m_action = SkillAction.Skill;
        }

        // 大招起手：记录原相机并启动演出
        if (m_action == SkillAction.FinishSkill)
        {
            player.PrepareFinishSkillCamera();
            player.BeginFinishSkillCinematic();
        }

        PlayAction(player);

        // 强化特殊技消耗能量
        if (m_action == SkillAction.SpecialSkill)
        {
            player.ConsumeSpecialSkillEnergy();
        }
    }

    protected override void OnExit(Player player)
    {
        // 退出时统一恢复相机与场景
        player.RestoreFinishSkillCamera();
        player.EndFinishSkillCinematic();
        m_currentAnimName = null;
    }

    protected override void OnStep(Player player)
    {
        // 大招的命中由演出导演统一结算
        if (m_action == SkillAction.FinishSkill)
        {
            player.TickFinishSkillCinematic(GetFinishSkillNormalizedTime(player));
        }
        else
        {
            TickHitWindow(player);
        }

        // 大招播完后自动衔接收尾动画
        if (m_action == SkillAction.FinishSkill && IsActionAnimationFinished(player))
        {
            if (HasConfiguredAction(player, SkillAction.FinishSkillPass))
            {
                ChangeAction(player, SkillAction.FinishSkillPass);
            }
            else
            {
                ExitToMoveOrIdle(player);
            }
            return;
        }

        // 动画播完直接退出
        if (IsActionAnimationFinished(player))
        {
            ExitToMoveOrIdle(player);
            return;
        }

        // 动画未正常播放时直接退出
        if (!IsActionAnimationActive(player))
        {
            ExitToMoveOrIdle(player);
        }
    }

    private void ChangeAction(Player player, SkillAction action)
    {
        m_action = action;
        PlayAction(player);
    }

    private void PlayAction(Player player)
    {
        // 收尾动作与已闪现的大招不再吸附
        if (m_action != SkillAction.FinishSkill && m_action != SkillAction.FinishSkillPass)
        {
            player.TryAttackSnap();
        }

        var animName = GetAnimName(player, m_action);
        if (string.IsNullOrEmpty(animName) || player.m_animator == null)
        {
            // 动画名未配置：OnStep 会直接退出
            m_currentAnimName = null;
            return;
        }

        // Animator 中不存在该状态时不播放，避免持续报错
        if (!player.m_animator.HasState(0, Animator.StringToHash(animName)))
        {
            Debug.LogWarning("[Player_SkillState] Animator 中不存在状态 " + animName + "，已跳过动作 " + m_action);
            m_currentAnimName = null;
            return;
        }

        m_currentAnimName = animName;
        player.m_animator.CrossFadeInFixedTime(animName, k_CrossFadeTime);
        player.ResetHitDetection();
    }

    /// <summary>finishskill 当前播放进度，不可用时返回 -1。</summary>
    private float GetFinishSkillNormalizedTime(Player player)
    {
        var animator = player.m_animator;
        if (animator == null || string.IsNullOrEmpty(m_currentAnimName) || animator.IsInTransition(0))
        {
            return -1f;
        }

        var info = animator.GetCurrentAnimatorStateInfo(0);
        return info.IsName(m_currentAnimName) ? info.normalizedTime : -1f;
    }

    private string GetAnimName(Player player, SkillAction action)
    {
        var stats = player.stats != null ? player.stats.current : null;
        if (stats == null)
        {
            return null;
        }

        switch (action)
        {
            case SkillAction.Skill: return stats.skillName;
            case SkillAction.SpecialSkill: return stats.specialSkillName;
            case SkillAction.AssistSkill: return stats.assistSkillName;
            case SkillAction.FinishSkill: return stats.finishSkillName;
            case SkillAction.FinishSkillPass: return stats.finishSkillPassName;
            default: return null;
        }
    }

    private void TickHitWindow(Player player)
    {
        if (player.m_animator == null || string.IsNullOrEmpty(m_currentAnimName))
        {
            return;
        }

        // 收尾动作不参与命中结算
        if (m_action == SkillAction.None || m_action == SkillAction.FinishSkillPass)
        {
            return;
        }

        var info = player.m_animator.GetCurrentAnimatorStateInfo(0);
        if (info.IsName(m_currentAnimName))
        {
            float damage = player.GetSkillAttackDamage(m_action);
            bool causesStun = player.IsStunCausingSkill(m_action);
            player.TickHitDetection(info.normalizedTime, damage, causesStun);
        }
    }

    private bool IsActionAnimationActive(Player player)
    {
        var animator = player.m_animator;
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

    private bool IsActionAnimationFinished(Player player)
    {
        var animator = player.m_animator;
        if (animator == null || string.IsNullOrEmpty(m_currentAnimName))
        {
            return false;
        }

        float exitTime = m_action == SkillAction.FinishSkill ? k_FinishSkillExitTime : k_ExitTime;
        var info = animator.GetCurrentAnimatorStateInfo(0);
        return info.IsName(m_currentAnimName) && info.normalizedTime >= exitTime;
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
}
