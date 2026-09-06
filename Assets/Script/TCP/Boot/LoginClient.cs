using System;
using UnityEngine;

/// <summary>登录客户端：发送登录请求并解析结果。</summary>
public class LoginClient : Client
{
    public event Action<string, string> OnLoginSuccess;
    public event Action<string> OnLoginFailed;

    protected override void Awake()
    {
        base.Awake();            
        OnMessageReceived += HandleMessage;
    }

    protected override void OnDestroy()
    {
        OnMessageReceived -= HandleMessage;
        base.OnDestroy();
    }

    /// <summary>发送登录请求。</summary>
    public void SendLogin(string account, string password, string serverId)
    {
        if (!connected)
        {
            OnLoginFailed?.Invoke("Not connected to the server");
            return;
        }

        var request = new LoginRequest
        {
            account = account,
            password = password,
            serverId = serverId
        };

        string json = JsonUtility.ToJson(request);
        base.Send(json);
    }

    /// <summary>解析登录结果并触发相应事件。</summary>
    private void HandleMessage(string message)
    {
        try
        {
            var response = JsonUtility.FromJson<LoginResponse>(message);
            if (response == null || response.type != "login_result")
                return;

            if (response.success)
            {
                OnLoginSuccess?.Invoke(response.playerName, response.token);
            }
            else
            {
                string reason = string.IsNullOrEmpty(response.reason) ? "Login failed" : response.reason;
                OnLoginFailed?.Invoke(reason);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"解析登录响应异常: {e.Message}\n原始消息: {message}");
        }
    }

    
}
