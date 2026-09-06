using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 实体状态基类：定义进入 / 退出 / 步进 / 碰撞生命周期，
/// 并支持按类名字符串反射创建，便于在 Inspector 配置状态列表。
/// </summary>
public abstract class EntityState<T> where T : Entity<T>
{
    public UnityEvent onEnter;

    public UnityEvent onExit;
    public float timeSinceEntered { get; protected set; }

    /// <summary>进入状态：重置计时并调用子类实现。</summary>
    public void Enter(T entity)
    {
        timeSinceEntered = 0;
        onEnter?.Invoke();
        OnEnter(entity);
    }

    /// <summary>退出状态：调用子类实现。</summary>
    public void Exit(T entity)
    {
        onExit?.Invoke();
        OnExit(entity);
    }

    /// <summary>每帧步进：调用子类实现并累计停留时间。</summary>
    public virtual void Step(T entity)
    {
        OnStep(entity);
        timeSinceEntered += Time.deltaTime;
    }

    protected abstract void OnEnter(T entity);
    protected abstract void OnExit(T entity);
    protected abstract void OnStep(T entity);
    protected abstract void OnContact(T entity, Collider other);

    /// <summary>按类型名反射创建状态实例。</summary>
    public static EntityState<T> CreateFromString(string typeName)
    {
        var type = System.Type.GetType(typeName);
        if (type == null)
        {
            Debug.LogError($"[EntityState] 找不到状态类型：{typeName}，请检查类名是否拼写正确。");
            return null;
        }
        return (EntityState<T>)System.Activator.CreateInstance(type);
    }

    /// <summary>按字符串数组批量创建状态列表，跳过无效类型。</summary>
    public static List<EntityState<T>> CreateListFromStringArray(string[] array)
    {
        var list = new List<EntityState<T>>();

        foreach (var typeName in array)
        {
            var state = CreateFromString(typeName);
            if (state != null)
            {
                list.Add(state);
            }
        }

        return list;
    }
}
