using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 场上活跃玩家的注册表：玩家启用时注册、停用时注销，
/// 供索敌 / 打击感系统复用，避免重复全场景查找。
/// </summary>
public static class PlayerRegistry
{
    private static readonly List<Player> s_live = new List<Player>(8);

    public static int Count => s_live.Count;

    public static Player GetAt(int index) => s_live[index];

    public static void Register(Player player)
    {
        if (player == null || s_live.Contains(player))
        {
            return;
        }
        s_live.Add(player);
    }

    public static void Unregister(Player player)
    {
        if (player != null)
        {
            s_live.Remove(player);
        }
    }

    /// <summary>查找半径内最近的存活玩家。</summary>
    public static Player FindNearest(Vector3 origin, float maxDistance)
    {
        Player best = null;
        float bestSqr = maxDistance * maxDistance;

        for (int i = 0; i < s_live.Count; i++)
        {
            var player = s_live[i];
            if (player == null
                || player.gameObject == null
                || !player.gameObject.activeInHierarchy
                || player.isDead)
            {
                continue;
            }

            var offset = player.transform.position - origin;
            offset.y = 0f;
            float sqr = offset.sqrMagnitude;
            if (sqr <= bestSqr)
            {
                bestSqr = sqr;
                best = player;
            }
        }
        return best;
    }
}
