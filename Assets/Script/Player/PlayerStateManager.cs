using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Player))]
public class PlayerStateManager : EntityStateManager<Player>
{
    [ClassTypeName(typeof(PlayerState))]
    public string[] states;

    protected override List<EntityState<Player>> GetStateList()
    {
        if (states == null || states.Length == 0)
        {
            return new List<EntityState<Player>>();
        }

        var list = PlayerState.CreateListFromStringArray(states);

        // 受击是战斗必需状态：配置漏填时自动补注册
        AppendCoreStateIfMissing(list, typeof(Player_HitState).Name);
        return list;
    }

    private static void AppendCoreStateIfMissing(List<EntityState<Player>> list, string typeName)
    {
        if (list == null)
        {
            return;
        }

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null && list[i].GetType().Name == typeName)
            {
                return;
            }
        }

        var state = PlayerState.CreateFromString(typeName);
        if (state != null)
        {
            list.Add(state);
        }
    }
}
