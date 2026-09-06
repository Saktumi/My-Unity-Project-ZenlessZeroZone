using System;
using UnityEngine;

/// <summary>频道聊天服务器：把收到的聊天消息广播给所有客户端。</summary>
public class LobbyChatServer : Server
{
    /// <summary>与登录服务器端口错开。</summary>
    public const int ChatPort = 54011;

    private void OnEnable()
    {
        OnMessageReceived += HandleMessage;
    }

    private void OnDisable()
    {
        OnMessageReceived -= HandleMessage;
    }

    private void Start()
    {
        ipAddress = "127.0.0.1";
        port = ChatPort;
        StartServer();
        GameLog.Log("[LobbyChatServer] 聊天服务器已启动 (127.0.0.1:54011)");
    }

    /// <summary>处理客户端消息，仅广播聊天内容。</summary>
    private void HandleMessage(string clientId, string message)
    {
        try
        {
            var chat = JsonUtility.FromJson<ChatMessage>(message);
            if (chat == null)
            {
                return;
            }

            // 客户端加入时先回发欢迎消息
            if (chat.type == ChatMessage.TypeJoin)
            {
                string sender = string.IsNullOrEmpty(chat.sender) ? "未知访客" : chat.sender;
                var welcome = new ChatMessage
                {
                    type = ChatMessage.TypeChat,
                    channel = ChatMessage.ChannelWorld,
                    sender = ChatMessage.SystemSender,
                    text = $"欢迎（{sender}）进入聊天",
                };
                SendTo(clientId, JsonUtility.ToJson(welcome));
                GameLog.Log($"[LobbyChatServer] {clientId} 进入聊天，已发送欢迎消息: 欢迎（{sender}）进入聊天");
                return;
            }

            if (chat.type != ChatMessage.TypeChat)
            {
                return; // 其他类型忽略
            }

            chat.text = (chat.text ?? string.Empty).Trim();
            if (chat.text.Length == 0)
            {
                return;
            }
            if (chat.text.Length > 500)
            {
                chat.text = chat.text.Substring(0, 500);
            }

            // 客户端不能伪造系统消息
            if (chat.channel == ChatMessage.ChannelSystem)
            {
                chat.channel = ChatMessage.ChannelWorld;
            }
            if (string.IsNullOrEmpty(chat.channel))
            {
                chat.channel = ChatMessage.ChannelWorld;
            }

            GameLog.Log($"[LobbyChatServer] [{chat.channel}] {chat.sender}: {chat.text}");
            Broadcast(JsonUtility.ToJson(chat));
        }
        catch (Exception e)
        {
            Debug.LogError($"[LobbyChatServer] 处理消息异常: {e.Message}");
        }
    }
}
