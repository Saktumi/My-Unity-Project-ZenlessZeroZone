using UnityEngine;

/// <summary>属性数据基类：玩家 / 敌人属性 ScriptableObject 的公共父类。</summary>
public abstract class EntityStats<T> : ScriptableObject where T : ScriptableObject {}
