using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 世界频道聊天窗口：系统消息直接显示在世界频道，
/// 消息使用中文 ttf 渲染。
/// </summary>
public class ChatPanel : MonoBehaviour
{
    private LobbyChatClient m_client;

    private Text m_statusText;
    private ScrollRect m_scroll;
    private RectTransform m_content;
    private TMP_InputField m_input;

    private readonly List<string> m_lines = new List<string>();

    private void Awake()
    {
        var bg = transform.Find("ChatBg");
        if (bg != null)
        {
            m_statusText = bg.Find("StatusText")?.GetComponent<Text>();
            m_input = bg.Find("ChatInput")?.GetComponent<TMP_InputField>();
            var scroll = bg.Find("MessageScroll");
            if (scroll != null)
            {
                m_scroll = scroll.GetComponent<ScrollRect>();
                m_content = scroll.Find("Viewport/Content") as RectTransform;
            }
        }

        if (m_input != null)
        {
            m_input.onSubmit.AddListener(_ => Send());
        }

        m_client = FindObjectOfType<LobbyChatClient>();
        if (m_client != null)
        {
            m_client.OnChatMessage += HandleMessage;
            m_client.OnClientStarted += HandleStarted;
            m_client.OnConnectionFailed += HandleConnectionFailed;
            m_client.OnDisconnected += HandleDisconnected;

            // 服务器已发过欢迎语则先取回显示
            if (m_client.welcomeMessage != null)
            {
                var welcome = m_client.welcomeMessage;
                m_client.welcomeMessage = null;
                HandleMessage(welcome);
            }
        }

        if (m_client != null && m_client.connected)
        {
            SetStatus("已连接频道服务器");
        }
    }

    private void OnDestroy()
    {
        if (m_client != null)
        {
            m_client.OnChatMessage -= HandleMessage;
            m_client.OnClientStarted -= HandleStarted;
            m_client.OnConnectionFailed -= HandleConnectionFailed;
            m_client.OnDisconnected -= HandleDisconnected;
        }
    }

    // ===== 场景按钮绑定 =====

    public void Show()
    {
        gameObject.SetActive(true);
        RefreshChannelView();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void Send()
    {
        if (m_input == null) return;
        string text = m_input.text;
        if (string.IsNullOrEmpty(text.Trim()))
        {
            return;
        }
        if (m_client == null || !m_client.connected)
        {
            SetStatus("尚未连接服务器，无法发送");
            return;
        }

        m_client.SendChat(ChatMessage.ChannelWorld, text);
        m_input.text = string.Empty;
        m_input.ActivateInputField();
    }

    public void Reconnect()
    {
        SetStatus("正在重新连接…");
        m_client?.Reconnect();
    }

    /// <summary>重建世界频道消息列表（打开窗口/收到消息后调用）。</summary>
    public void RefreshChannelView()
    {
        if (m_content == null) return;

        for (int i = m_content.childCount - 1; i >= 0; i--)
        {
            Destroy(m_content.GetChild(i).gameObject);
        }

        for (int i = 0; i < m_lines.Count; i++)
        {
            AddLineRaw(m_lines[i]);
        }
        StartCoroutine(ScrollToBottomNextFrame());
    }

    // ===== 网络事件 =====

    private void HandleStarted()
    {
        SetStatus("已连接频道服务器");
    }

    private void HandleConnectionFailed(string reason)
    {
        SetStatus($"连接失败：{reason}（可点“重新连接”重试）");
    }

    private void HandleDisconnected()
    {
        SetStatus("与频道服务器断开（可点“重新连接”重试）");
    }

    private void HandleMessage(ChatMessage message)
    {
        if (message == null) return;

        string time = DateTime.Now.ToString("HH:mm:ss");
        string safe = (message.text ?? string.Empty).Replace("<", "＜").Replace(">", "＞");

        // 系统消息使用系统发送者名称
        string sender;
        if (message.channel == ChatMessage.ChannelSystem ||
            string.Equals(message.sender, ChatMessage.SystemSender, StringComparison.OrdinalIgnoreCase))
        {
            sender = ChatMessage.SystemSender;
        }
        else
        {
            sender = string.IsNullOrEmpty(message.sender) ? "未知" : message.sender;
        }

        // 发送者 + 时间，第二行消息内容
        string line = $"{sender}  {time}\n{safe}";
        m_lines.Add(line);
        AddLineRaw(line);
    }

    private void AddLineRaw(string line)
    {
        if (m_content == null) return;

        var go = new GameObject("Line", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(m_content, false);

        // 使用中文 ttf 的 legacy Text 渲染
        var text = go.AddComponent<Text>();
        text.font = LobbyUI.chineseSourceFont;
        text.fontSize = 22;
        text.color = new Color(0.92f, 0.93f, 0.95f);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.text = line;

        var fitter = go.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        StartCoroutine(ScrollToBottomNextFrame());
    }

    private IEnumerator ScrollToBottomNextFrame()
    {
        yield return null;
        if (m_scroll == null || m_content == null) yield break;
        if (m_scroll.content == null)
        {
            m_scroll.content = m_content;
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(m_content);
        m_scroll.verticalNormalizedPosition = 0f;
    }

    private void SetStatus(string text)
    {
        if (m_statusText != null)
        {
            m_statusText.text = text;
        }
    }
}
