using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class EnemyStateManager : EntityStateManager<Enemy>
{
    [ClassTypeName(typeof(EnemyState))]
    public string[] states;

    protected override List<EntityState<Enemy>> GetStateList()
    {
        if (states == null || states.Length == 0)
        {
            return new List<EntityState<Enemy>>();
        }

        return EnemyState.CreateListFromStringArray(states);
    }

    /// <summary>死亡锁定：生命归零后除死亡状态外禁止任何切换。</summary>
    public override void Change(EntityState<Enemy> to)
    {
        if (entity != null && entity.deathLocked && !(to is Enemy_DeathState))
        {
            return;
        }

        base.Change(to);
    }
}
