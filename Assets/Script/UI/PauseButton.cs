using UnityEngine;

/// <summary>暂停按钮：恢复与切换，供 UI OnClick 绑定。</summary>
public class PauseButton : MonoBehaviour
{
    public void Resume() => LevelPauser.instance.Pause(false);
    public void Toggle() => LevelPauser.instance.Pause(!LevelPauser.instance.paused);
}
