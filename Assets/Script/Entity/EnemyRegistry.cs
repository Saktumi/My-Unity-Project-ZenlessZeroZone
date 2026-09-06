using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 场上活跃敌人的注册表：敌人启用时注册、停用时注销，
/// 供命中检测 / 索敌 / 吸附直接遍历，避免每帧全场景查找。
/// </summary>
public static class EnemyRegistry
{
    private static readonly List<Enemy> s_live = new List<Enemy>(16);

    public static int Count => s_live.Count;

    public static Enemy GetAt(int index) => s_live[index];

    public static void Register(Enemy enemy)
    {
        if (enemy == null || s_live.Contains(enemy))
        {
            return;
        }
        s_live.Add(enemy);
    }

    public static void Unregister(Enemy enemy)
    {
        if (enemy != null)
        {
            s_live.Remove(enemy);
        }
    }

    /// <summary>是否可作为攻击 / 吸附目标。</summary>
    public static bool IsUsableTarget(Enemy enemy)
    {
        return enemy != null
            && enemy.gameObject != null
            && enemy.gameObject.activeInHierarchy
            && !enemy.isDead
            && (enemy.controller == null || enemy.controller.enabled);
    }

    /// <summary>在半径内找最近的可命中敌人（水平距离）。</summary>
    public static Enemy FindNearest(Vector3 origin, float maxDistance)
    {
        Enemy best = null;
        float bestSqr = maxDistance * maxDistance;

        for (int i = 0; i < s_live.Count; i++)
        {
            var enemy = s_live[i];
            if (!IsUsableTarget(enemy))
            {
                continue;
            }

            var offset = enemy.transform.position - origin;
            offset.y = 0f;
            float sqr = offset.sqrMagnitude;
            if (sqr <= bestSqr)
            {
                bestSqr = sqr;
                best = enemy;
            }
        }
        return best;
    }

    /// <summary>在扇形范围内找最近的可命中敌人（origin 玩家位置，forward 玩家朝向）。</summary>
    public static Enemy FindNearestInFront(Vector3 origin, Vector3 forward, float maxDistance, float maxAngle)
    {
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
        {
            return FindNearest(origin, maxDistance);
        }
        forward.Normalize();

        Enemy best = null;
        float bestSqr = maxDistance * maxDistance;

        for (int i = 0; i < s_live.Count; i++)
        {
            var enemy = s_live[i];
            if (!IsUsableTarget(enemy))
            {
                continue;
            }

            var offset = enemy.transform.position - origin;
            offset.y = 0f;
            float sqr = offset.sqrMagnitude;
            if (sqr > bestSqr)
            {
                continue;
            }

            if (Vector3.Angle(forward, offset.normalized) > maxAngle)
            {
                continue;
            }

            bestSqr = sqr;
            best = enemy;
        }
        return best;
    }
}
