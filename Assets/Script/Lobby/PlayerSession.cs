using UnityEngine;

/// <summary>跨场景玩家会话数据：玩家名与登录令牌。</summary>
public static class PlayerSession
{
    public static string playerName = "游客";
    public static string token = string.Empty;

    /// <summary>用户名，聊天发送者用它显示。</summary>
    public static string username => playerName;

    public static void Set(string name, string loginToken)
    {
        playerName = string.IsNullOrEmpty(name) ? "游客" : name;
        token = loginToken ?? string.Empty;
    }

    public static void Reset()
    {
        playerName = "游客";
        token = string.Empty;
    }
}
