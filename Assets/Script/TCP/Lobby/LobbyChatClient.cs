using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 频道聊天客户端：自动连接聊天服务器，
/// 发送消息并派发收到的聊天事件。
/// </summary>
public class LobbyChatClient : Client
{
    public const int ChatPort = 54011;

    /// <summary>收到一条聊天消息时触发。</summary>
    public event Action<ChatMessage> OnChatMessage;

    /// <summary>最近一条系统欢迎消息，供界面取走显示。</summary>
    public ChatMessage welcomeMessage;

    public bool autoConnectOnStart = true;

    protected override void Awake()
    {
        base.Awake();
        ipAddress = "127.0.0.1";
        port = ChatPort;
        OnMessageReceived += HandleMessage;
        OnClientStarted += HandleStarted;
    }

    private void Start()
    {
        if (autoConnectOnStart)
        {
            // 稍作延迟，确保同帧创建的服务器先完成监听
            StartCoroutine(AutoConnectLater());
        }
    }

    private IEnumerator AutoConnectLater()
    {
        yield return new WaitForSeconds(0.6f);
        if (autoConnectOnStart && !connected && !m_running)
        {
            StartClient();
        }
    }

    protected override void OnDestroy()
    {
        OnMessageReceived -= HandleMessage;
        OnClientStarted -= HandleStarted;
        base.OnDestroy();
    }

    /// <summary>连接成功后向服务器报到，服务器会回发欢迎消息。</summary>
    private void HandleStarted()
    {
        SendJoin();
    }

    /// <summary>发送“加入聊天”握手消息。</summary>
    public void SendJoin()
    {
        if (!connected)
        {
            return;
        }

        var join = new ChatMessage
        {
            type = ChatMessage.TypeJoin,
            channel = ChatMessage.ChannelWorld,
            sender = string.IsNullOrEmpty(PlayerSession.username) ? "游客" : PlayerSession.username,
        };
        base.Send(JsonUtility.ToJson(join));
    }

    /// <summary>发送一条频道聊天消息。</summary>
    public void SendChat(string channel, string text)
    {
        if (!connected)
        {
            Debug.LogWarning("[LobbyChatClient] 未连接，无法发送");
            return;
        }

        text = (text ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            return;
        }
        if (string.IsNullOrEmpty(channel))
        {
            channel = ChatMessage.ChannelWorld;
        }

        var msg = new ChatMessage
        {
            channel = channel,
            sender = string.IsNullOrEmpty(PlayerSession.username) ? "游客" : PlayerSession.username,
            text = text,
        };
        base.Send(JsonUtility.ToJson(msg));
    }

    /// <summary>断开后重新连接。</summary>
    public void Reconnect()
    {
        if (connected || m_running)
        {
            return;
        }
        StartClient();
    }

    private void HandleMessage(string message)
    {
        try
        {
            var chat = JsonUtility.FromJson<ChatMessage>(message);
            if (chat == null || chat.type != "chat")
            {
                return;
            }
            if (chat.channel == ChatMessage.ChannelSystem)
            {
                welcomeMessage = chat;
            }
            OnChatMessage?.Invoke(chat);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LobbyChatClient] 解析消息异常: {e.Message}\n{message}");
        }
    }
}
