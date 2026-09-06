using UnityEngine;

/// <summary>
/// 敌人状态基类：统一推进攻击冷却，
/// 并按 死亡 > 眩晕 > 受击 > 攻击 的优先级响应请求。
/// </summary>
public abstract class EnemyState : EntityState<Enemy>
{
    public override void Step(Enemy enemy)
    {
        base.Step(enemy);

        enemy.TickAttackCooldown();

        if (enemy.states.current is Enemy_DeathState)
        {
            return;
        }

        if (TryEnterDeath(enemy))
        {
            return;
        }

        if (TryEnterStun(enemy))
        {
            return;
        }
        if (TryEnterHit(enemy))
        {
            return;
        }

        // 硬直 / 攻击动画期间不进入新的攻击
        if (enemy.states.current is Enemy_AttackState
            || enemy.states.current is Enemy_HitState
            || enemy.states.current is Enemy_StunState)
        {
            return;
        }

        if (TryEnterAttack(enemy))
        {
            return;
        }
    }

    protected virtual bool TryEnterDeath(Enemy enemy)
    {
        if (!enemy.ConsumeDeathRequest())
        {
            return false;
        }

        enemy.states.Change<Enemy_DeathState>();
        return true;
    }

    protected virtual bool TryEnterStun(Enemy enemy)
    {
        if (!enemy.ConsumeStunRequest())
        {
            return false;
        }

        enemy.states.Change<Enemy_StunState>();
        return true;
    }

    protected virtual bool TryEnterHit(Enemy enemy)
    {
        if (!enemy.ConsumeHitRequest())
        {
            return false;
        }

        enemy.states.Change<Enemy_HitState>();
        return true;
    }

    protected virtual bool TryEnterAttack(Enemy enemy)
    {
        if (!enemy.IsAttackRequested())
        {
            return false;
        }

        enemy.ConsumeAttackRequest();
        enemy.states.Change<Enemy_AttackState>();
        return true;
    }
}
