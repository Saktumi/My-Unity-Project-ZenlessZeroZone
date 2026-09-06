using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>服务器 UI 封装：按钮控制启停，事件更新状态显示。</summary>
public class CustomServer : Server
{
    [Header("UI引用")]
    [SerializeField] private Button m_StartServerButton = null;
    [SerializeField] private Button m_CloseServerButton = null;
    [SerializeField] private TMP_Text m_StatusText = null;

    protected virtual void Awake()
    {
        if (m_StartServerButton == null || m_CloseServerButton == null)
        {
            Debug.LogError("[CustomServer] 请把启动/关闭按钮拖到 Inspector");
            return;
        }

        m_StartServerButton.onClick.AddListener(StartServer);
        m_CloseServerButton.onClick.AddListener(CloseServer);

        OnServerStarted += HandleServerStarted;
        OnServerStopped += HandleServerStopped;
        OnClientConnected += HandleClientConnected;
        OnClientDisconnected += HandleClientDisconnected;
        OnMessageReceived += HandleMessage;

        SetButtons(false);
        SetStatus("Server not started");
    }

    protected override void OnDestroy()
    {
        OnServerStarted -= HandleServerStarted;
        OnServerStopped -= HandleServerStopped;
        OnClientConnected -= HandleClientConnected;
        OnClientDisconnected -= HandleClientDisconnected;
        OnMessageReceived -= HandleMessage;

        base.OnDestroy();
    }

    private void HandleServerStarted()
    {
        SetButtons(true);
        SetStatus("Server started, waiting for clients...");
    }

    private void HandleServerStopped()
    {
        SetButtons(false);
        SetStatus("Server closed");
    }

    private void HandleClientConnected()
    {
        SetStatus($"Client connected (currently {clientCount})");
    }

    private void HandleClientDisconnected(string clientId)
    {
        SetStatus($"Client disconnected: {clientId} (currently {clientCount})");
    }

    private void HandleMessage(string clientId, string message)
    {
        SetStatus($"From {clientId}: {message}");
    }

    private void SetButtons(bool running)
    {
        if (m_StartServerButton != null)
        {
            m_StartServerButton.interactable = !running;
        }
        if (m_CloseServerButton != null)
        {
            m_CloseServerButton.interactable = running;
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
