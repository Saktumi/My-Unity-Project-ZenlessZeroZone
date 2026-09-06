using UnityEngine;

/// <summary>
/// 统一日志出口：普通日志在 Editor / Development Build 才输出且默认关闭，
/// 警告与错误保持常开，便于定位配置和运行时问题。
/// </summary>
public static class GameLog
{
    private const string k_EnabledPrefsKey = "ZZZ.GameLog.Enabled";

    /// <summary>普通日志总开关，默认关闭。</summary>
    public static bool logEnabled { get; set; } = LoadEnabledFromPrefs();

    /// <summary>普通调试日志：Editor / Development Build 才输出。</summary>
    public static void Log(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (logEnabled)
        {
            Debug.Log(message);
        }
#endif
    }

    /// <summary>带上下文标签的普通调试日志。</summary>
    public static void Log(string context, object message)
    {
        Log($"[{context}] {message}");
    }

    /// <summary>警告：常开。</summary>
    public static void Warn(object message)
    {
        Debug.LogWarning(message);
    }

    /// <summary>错误：常开。</summary>
    public static void Error(object message)
    {
        Debug.LogError(message);
    }

    /// <summary>异常：常开。</summary>
    public static void Exception(System.Exception exception)
    {
        Debug.LogException(exception);
    }

    private static bool LoadEnabledFromPrefs()
    {
        return PlayerPrefs.GetInt(k_EnabledPrefsKey, 0) == 1;
    }

    /// <summary>供编辑器菜单切换并持久化开关状态。</summary>
    public static void SetEnabledFromEditor(bool value)
    {
        logEnabled = value;
        PlayerPrefs.SetInt(k_EnabledPrefsKey, value ? 1 : 0);
    }
}
