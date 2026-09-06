using System;

/// <summary>
/// 聊天消息协议：type 区分消息类型，JsonUtility 序列化；
/// channel 为 world / team / system。
/// </summary>
[Serializable]
public class ChatMessage
{
    public string type = TypeChat;
    public string channel = "world";
    public string sender;
    public string text;

    /// <summary>普通聊天消息。</summary>
    public const string TypeChat = "chat";
    /// <summary>加入聊天的握手消息。</summary>
    public const string TypeJoin = "join";

    public const string ChannelWorld = "world";
    public const string ChannelTeam = "team";
    public const string ChannelSystem = "system";

    /// <summary>系统消息的发送者名称。</summary>
    public const string SystemSender = "siri";
}
