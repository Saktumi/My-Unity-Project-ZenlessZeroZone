using UnityEngine;

/// <summary>属性管理器：持有属性列表，可在运行中切换当前属性。</summary>
public class EntityStatsManager<T> : MonoBehaviour where T : EntityStats<T>
{
    public T[] stats;
    public T current { get; protected set; }

    /// <summary>切换到指定下标的属性。</summary>
    public virtual void Change(int to)
    {
        if (stats != null && to >= 0 && to < stats.Length)
        {
            if (current != stats[to])
            {
                current = stats[to];
            }
        }
    }

    /// <summary>直接指定当前属性，不修改原资产。</summary>
    public virtual void SetCurrent(T value)
    {
        if (value != null)
        {
            current = value;
        }
    }

    protected virtual void Start()
    {
        if (stats != null && stats.Length > 0)
        {
            current = stats[0];
        }
    }
}
