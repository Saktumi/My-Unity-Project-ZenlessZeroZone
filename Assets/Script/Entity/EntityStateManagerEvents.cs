using System;
using UnityEngine.Events;

/// <summary>状态事件容器，供 Inspector 绑定切换回调。</summary>
[Serializable]
public class EntityStateManagerEvents
{
    public UnityEvent onChange;
    public UnityEvent<Type> onEnter;
    public UnityEvent<Type> onExit;
}
