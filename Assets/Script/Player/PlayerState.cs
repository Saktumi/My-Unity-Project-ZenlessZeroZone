using UnityEngine;

/// <summary>
/// 玩家状态基类：统一处理受击打断、技能锁定与输入状态切换。
/// </summary>
public abstract class PlayerState : EntityState<Player>
{
    public override void Step(Player player)
    {
        // 切人入场期间锁定状态机，避免入场动画被打断
        if (player.IsSwitchingIn)
        {
            return;
        }

        // 受击优先级最高；硬直中不重复进入
        if (player.HasPendingHit && !(player.states.current is Player_HitState))
        {
            // 受击瞬间按闪避可取消本次受击
            if (TryEnterEvade(player))
            {
                player.ConsumeHitRequest();
                return;
            }

            player.states.Change<Player_HitState>();
            return;
        }

        bool wasHitState = player.states.current is Player_HitState;
        base.Step(player);

        // 技能 / 受击动画播完前不允许其它动作打断
        if (player.states.current is Player_SkillState || player.states.current is Player_HitState)
        {
            return;
        }

        // 刚用闪避取消受击后不再处理其它输入
        if (wasHitState && player.states.current is Player_EvadeState)
        {
            return;
        }

        if (TryEnterEvade(player))
        {
            return;
        }
        if (TryEnterSkill(player))
        {
            return;
        }
         if (player.inputs != null && player.inputs.IsNormalAttackPressed()
             && !(player.states.current is Player_NormalAttackState))
        {
            player.states.Change<Player_NormalAttackState>();
        }
    }

    protected virtual bool TryEnterSkill(Player player)
    {
        if (player.inputs == null)
        {
            return false;
        }

        if (player.states.current is Player_SkillState)
        {
            return false;
        }

        // Q：终结技
        if (player.inputs.IsFinishSkillPressed()
            && Player_SkillState.HasConfiguredAction(player, Player_SkillState.SkillAction.FinishSkill))
        {
            player.RequestSkill(Player_SkillState.SkillAction.FinishSkill);
            player.states.Change<Player_SkillState>();
            return true;
        }

        // E：能量足够时播强化特殊技
        if (player.inputs.IsSkillPressed())
        {
            var action = player.CanUseSpecialSkill
                && Player_SkillState.HasConfiguredAction(player, Player_SkillState.SkillAction.SpecialSkill)
                    ? Player_SkillState.SkillAction.SpecialSkill
                    : Player_SkillState.SkillAction.Skill;

            if (Player_SkillState.HasConfiguredAction(player, action))
            {
                player.RequestSkill(action);
                player.states.Change<Player_SkillState>();
                return true;
            }
        }

        return false;
    }

    protected virtual bool TryEnterEvade(Player player)
    {
        if (player.inputs == null)
        {
            return false;
        }

        player.TickEvadeCooldown();

        if (!player.CanEvade)
        {
            return false;
        }

        if (!player.inputs.IsEvadePressed())
        {
            return false;
        }

        // 第一段闪避播放期间不响应再次输入
        if (player.states.current is Player_EvadeState)
        {
            return false;
        }

        // 衔接窗口内按下则为第二段闪避，触发冷却
        if (player.CanChainEvade)
        {
            player.StartEvadeCooldown();
        }

        player.states.Change<Player_EvadeState>();
        return true;
    }
}
