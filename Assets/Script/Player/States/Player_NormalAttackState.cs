
using UnityEngine;

public class Player_NormalAttackState : PlayerState
{
    private const float k_ChainThreshold = 0.5f;
    private const float k_CrossFadeTime = 0.05f;
    private const float k_LastHitHoldTime = 0.5f;

    private int m_comboIndex;
    private string m_currentAnimName;
    private int m_resumeComboIndex = -1;
    private float m_lastHitHoldTimer;
    private bool m_restartQueued;
    private bool m_isRushing;

    protected override void OnContact(Player player, Collider other)
    {
      
    }

    protected override void OnEnter(Player player)
    {
        if (player.m_animator == null)
        {
            return;
        }

        m_lastHitHoldTimer = 0f;
        m_restartQueued = false;

        var names = GetAttackNames(player);
        if (names != null && names.Length > 0 && m_resumeComboIndex >= 0)
        {
            // 闪避打断后从下一段继续
            m_comboIndex = (m_resumeComboIndex + 1) % names.Length;
            m_resumeComboIndex = -1;
            m_isRushing = false;
            PlayCurrentCombo(player);
            return;
        }

        m_comboIndex = 0;
        m_isRushing = false;

        // 闪避结束短窗口内攻击进入冲刺攻击分支
        if (CanUseRush(player))
        {
            player.ConsumeRushAttack();
            PlayRushAttack(player);
        }
        else
        {
            PlayCurrentCombo(player);
        }
    }

    protected override void OnExit(Player player)
    {
        // 覆盖所有退出路径，恢复根运动位移
        player.SetSuppressRootMotion(false);
        m_currentAnimName = null;
        m_lastHitHoldTimer = 0f;
        m_restartQueued = false;
    }

    // 闪避打断普攻时记录连击进度
    public void PreserveCombo()
    {
        // 冲刺攻击被打断则不保留进度
        m_resumeComboIndex = m_isRushing ? -1 : m_comboIndex;
    }

    protected override void OnStep(Player player)
    {
        var names = GetAttackNames(player);
        if (names == null || names.Length == 0)
        {
            ExitToMoveOrIdle(player);
            return;
        }

        // 命中窗口内检测并结算
        TickHitWindow(player);

        bool pressed = player.inputs != null && player.inputs.IsNormalAttackPressed();
        bool isLastHit = m_comboIndex == names.Length - 1;

        // 冲刺攻击播完直接退出，不接连击
        if (m_isRushing)
        {
            if (!IsAttackAnimationPlaying(player) || IsAttackAnimationFinished(player))
            {
                ExitToMoveOrIdle(player);
            }
            return;
        }

        // 最后一段结束后短暂停留，期间按下攻击则回到第一段
        if (isLastHit)
        {
            if (pressed)
            {
                m_restartQueued = true;
            }

            if (!IsAttackAnimationPlaying(player) || IsAttackAnimationFinished(player))
            {
                m_lastHitHoldTimer += Time.deltaTime;
                if (m_lastHitHoldTimer >= k_LastHitHoldTime)
                {
                    m_lastHitHoldTimer = 0f;
                    if (m_restartQueued)
                    {
                        m_restartQueued = false;
                        m_comboIndex = 0;
                        PlayCurrentCombo(player);
                    }
                    else
                    {
                        ExitToMoveOrIdle(player);
                    }
                }
                return;
            }
        }

        // 攻击达到衔接阈值后接下一段
        if (!isLastHit && pressed && IsAttackAnimationAt(player, k_ChainThreshold))
        {
            m_comboIndex = (m_comboIndex + 1) % names.Length;
            PlayCurrentCombo(player);
            return;
        }

        // 当前段播完退出连击状态
        if (!IsAttackAnimationPlaying(player) || IsAttackAnimationFinished(player))
        {
            ExitToMoveOrIdle(player);
        }
    }

    private string[] GetAttackNames(Player player)
    {
        var stats = player.stats != null ? player.stats.current : null;
        return stats != null ? stats.normalName : null;
    }

    private void TickHitWindow(Player player)
    {
        if (player.m_animator == null || string.IsNullOrEmpty(m_currentAnimName))
        {
            return;
        }

        var info = player.m_animator.GetCurrentAnimatorStateInfo(0);
        if (info.IsName(m_currentAnimName))
        {
            player.TickHitDetection(info.normalizedTime, player.GetNormalAttackDamage(), false);
        }
    }

    private string GetRushAttackName(Player player)
    {
        var stats = player.stats != null ? player.stats.current : null;
        return stats != null ? stats.rushAttackName : null;
    }

    private bool CanUseRush(Player player)
    {
        return player.CanRushAttack && !string.IsNullOrEmpty(GetRushAttackName(player));
    }

    private void PlayRushAttack(Player player)
    {
        var rushName = GetRushAttackName(player);
        if (string.IsNullOrEmpty(rushName) || player.m_animator == null)
        {
            return;
        }

        player.TryAttackSnap();
        player.SetSuppressRootMotion(false);

        m_isRushing = true;
        m_currentAnimName = rushName;
        player.m_animator.CrossFadeInFixedTime(rushName, k_CrossFadeTime);
        player.ResetHitDetection();
    }

    private void PlayCurrentCombo(Player player)
    {
        var names = GetAttackNames(player);
        if (names == null || names.Length == 0 || player.m_animator == null)
        {
            return;
        }

        player.TryAttackSnap();

        m_comboIndex = Mathf.Clamp(m_comboIndex, 0, names.Length - 1);
        m_currentAnimName = names[m_comboIndex];
        // 最后一段禁用根运动，避免被连击推进带离目标
        player.SetSuppressRootMotion(m_comboIndex >= names.Length - 1);
        player.m_animator.CrossFadeInFixedTime(m_currentAnimName, k_CrossFadeTime);
        player.ResetHitDetection();
    }

    private bool IsAttackAnimationAt(Player player, float normalizedTime)
    {
        var animator = player.m_animator;
        if (animator == null || string.IsNullOrEmpty(m_currentAnimName))
        {
            return false;
        }

        var info = animator.GetCurrentAnimatorStateInfo(0);
        return info.IsName(m_currentAnimName) && info.normalizedTime >= normalizedTime;
    }

    private bool IsAttackAnimationPlaying(Player player)
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

    private bool IsAttackAnimationFinished(Player player)
    {
        var animator = player.m_animator;
        if (animator == null || string.IsNullOrEmpty(m_currentAnimName))
        {
            return false;
        }

        var info = animator.GetCurrentAnimatorStateInfo(0);
        return info.IsName(m_currentAnimName) && info.normalizedTime >= 0.98f;
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
