using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>客户端 UI 封装：按钮控制连接 / 断开，事件更新状态。</summary>
public class CustomClient : Client
{
    [Header("UI引用")]
    [SerializeField] private Button m_StartClientButton = null;
    [SerializeField] private Button m_StopClientButton = null;
    [SerializeField] private TMP_InputField m_MessageInput = null;
    [SerializeField] private Button m_SendButton = null;
    [SerializeField] private TMP_Text m_StatusText = null;

    protected virtual void Awake()
    {
        if (m_StartClientButton == null || m_StopClientButton == null)
        {
            Debug.LogError("[CustomClient] 请把启动/停止按钮拖到 Inspector");
            return;
        }

        m_StartClientButton.onClick.AddListener(StartClient);
        m_StopClientButton.onClick.AddListener(StopClient);

        if (m_SendButton != null)
        {
            m_SendButton.onClick.AddListener(SendInputMessage);
        }

        OnClientStarted += HandleStarted;
        OnConnectionFailed += HandleConnectionFailed;
        OnDisconnected += HandleDisconnected;
        OnMessageReceived += HandleMessage;

        SetButtons(false);
        SetStatus("Not connected");
    }

    protected override void OnDestroy()
    {
        OnClientStarted -= HandleStarted;
        OnConnectionFailed -= HandleConnectionFailed;
        OnDisconnected -= HandleDisconnected;
        OnMessageReceived -= HandleMessage;

        base.OnDestroy();
    }

    private void SendInputMessage()
    {
        if (m_MessageInput == null)
        {
            return;
        }

        Send(m_MessageInput.text);
        m_MessageInput.text = string.Empty;
    }

    private void HandleStarted()
    {
        SetButtons(true);
        SetStatus("Connected");
    }

    private void HandleConnectionFailed(string reason)
    {
        SetButtons(false);
        SetStatus($"Connection failed: {reason}");
    }

    private void HandleDisconnected()
    {
        SetButtons(false);
        SetStatus("Disconnected");
    }

    private void HandleMessage(string message)
    {
        SetStatus($"Received: {message}");
    }

    private void SetButtons(bool connected)
    {
        if (m_StartClientButton != null)
        {
            m_StartClientButton.interactable = !connected;
        }
        if (m_StopClientButton != null)
        {
            m_StopClientButton.interactable = connected;
        }
    }

    private void SetStatus(string text)
    {
        if (m_StatusText != null)
        {
            m_StatusText.text = text;
        }
    }
}
