using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

/// <summary>关卡启动：锁定光标并在延迟后触发 OnStart。</summary>
public class LevelStarter : Singleton<LevelStarter>
{
    public UnityEvent OnStart;
    public float enablePlayerDelay = 1f;

    protected virtual void Start()
    {
        LevelManager.LockCursor();
        DOVirtual.DelayedCall(enablePlayerDelay, () => OnStart?.Invoke());
    }
}
