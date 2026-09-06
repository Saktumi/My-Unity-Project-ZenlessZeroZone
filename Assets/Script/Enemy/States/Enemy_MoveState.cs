using UnityEngine;

public class Enemy_MoveState : EnemyState
{
    protected override void OnEnter(Enemy enemy)
    {
        if (enemy.m_animator == null)
        {
            return;
        }

        var stats = enemy.stats != null ? enemy.stats.current : null;
        if (stats == null)
        {
            return;
        }

        // 追击且超出攻击范围时跑步，否则走路
        bool isRunning = enemy.HasTarget && enemy.DistanceToTarget > stats.attackRange;
        string animName = isRunning ? stats.runAnimName : stats.walkAnimName;

        if (string.IsNullOrEmpty(animName))
        {
            return;
        }

        enemy.m_animator.CrossFadeInFixedTime(animName, 0.15f);
    }

    protected override void OnExit(Enemy enemy) { }

    protected override void OnStep(Enemy enemy)
    {
        var direction = enemy.GetMovementDirection();

        if (direction.sqrMagnitude > 0.01f)
        {
            enemy.FaceDirectionSmooth(direction);
        }
        else
        {
            enemy.states.Change<Enemy_IdleState>();
        }
    }

    protected override void OnContact(Enemy enemy, Collider other) { }
}
