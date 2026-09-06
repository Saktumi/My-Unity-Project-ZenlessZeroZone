using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>状态管理器非泛型基类：持有可在 Inspector 绑定的事件对象。</summary>
public abstract class EntityStateManager : MonoBehaviour
{
    public EntityStateManagerEvents events;
}

/// <summary>
/// 泛型状态管理器：创建状态列表、逐帧步进并在切换时触发 UnityEvent。
/// </summary>
public abstract class EntityStateManager<T> : EntityStateManager where T : Entity<T>
{
    protected List<EntityState<T>> m_list = new List<EntityState<T>>();
    public EntityState<T> current { get; protected set; }
    public T entity { get; protected set; }
    protected Dictionary<Type, EntityState<T>> m_states = new Dictionary<Type, EntityState<T>>();
    public EntityState<T> last { get; protected set; }
    public int index => m_list.IndexOf(current);
    public int lastIndex => m_list.IndexOf(last);

    protected abstract List<EntityState<T>> GetStateList();

    protected virtual void Start()
    {
        InitializeEntity();
        InitializeStates();
    }

    /// <summary>获取同物体上的实体引用。</summary>
    protected virtual void InitializeEntity()
    {
        entity = GetComponent<T>();
    }

    /// <summary>根据字符串配置创建并注册状态，随后进入第一个状态。</summary>
    protected virtual void InitializeStates()
    {
        m_list = GetStateList();

        if (m_list == null || m_list.Count == 0)
        {
            return;
        }

        foreach (var state in m_list)
        {
            var type = state.GetType();
            if (!m_states.ContainsKey(type))
            {
                m_states.Add(type, state);
            }
        }

        current = m_list[0];
        Change(current);
    }

    /// <summary>每帧步进当前状态；timeScale 为 0（暂停）时不执行。</summary>
    public virtual void Step()
    {
        if (current != null && Time.timeScale > 0)
        {
            current.Step(entity);
        }
        else if (current == null)
        {
            if (m_list != null && m_list.Count > 0)
            {
                current = m_list[0];
            }
        }
    }

    /// <summary>按类型切换状态（未注册的类型会被忽略）。</summary>
    public virtual void Change<TState>() where TState : EntityState<T>
    {
        if (m_states.TryGetValue(typeof(TState), out var state))
        {
            Change(state);
        }
    }

    /// <summary>退出当前状态并进入新状态，同时触发切换事件。</summary>
    public virtual void Change(EntityState<T> to)
    {
        if (to != null && Time.timeScale > 0)
        {
            if (current != null)
            {
                current.Exit(entity);
                events?.onExit?.Invoke(current.GetType());
                last = current;
            }

            current = to;
            current.Enter(entity);
            events?.onEnter?.Invoke(current.GetType());
            events?.onChange?.Invoke();
        }
    }
}
