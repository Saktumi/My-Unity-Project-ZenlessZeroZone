using UnityEngine;
using UnityEngine.Events;

/// <summary>全局暂停管理：统一控制 timeScale 与暂停 UI。</summary>
public class LevelPauser : Singleton<LevelPauser>
{
    public UnityEvent OnPause;
    public UnityEvent OnUnpause;

    public UIAnimator pauseScreen;
    public bool canPause { get; set; } = true;
    public bool paused { get; protected set; }

    /// <summary>暂停 / 恢复游戏。</summary>
    public virtual void Pause(bool value)
    {
        if (paused != value)
        {
            if (!paused)
            {
                if (canPause)
                {
                    LevelManager.LockCursor(false);
                    paused = true;
                    Time.timeScale = 0;
                    pauseScreen.SetActive(true);
                    pauseScreen?.Show();
                    OnPause?.Invoke();
                }
            }
            else
            {
                LevelManager.LockCursor();
                paused = false;
                Time.timeScale = 1;
                pauseScreen?.Hide();
                OnUnpause?.Invoke();
            }
        }
    }
}
