using UnityEngine;

/// <summary>全局关卡工具：鼠标光标锁定 / 解锁。</summary>
public class LevelManager : Singleton<LevelManager>
{
    /// <summary>锁定或解锁鼠标光标。</summary>
    public static void LockCursor(bool value = true)
    {
        Cursor.visible = !value;
        Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
    }
}
