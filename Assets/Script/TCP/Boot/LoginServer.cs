using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>登录服务器（本地模拟）：校验账号密码并签发令牌。</summary>
public class LoginServer : Server
{
    private Dictionary<string, string> accountTable;

    private void Start()
    {
        accountTable = new Dictionary<string, string>
        {
            { "test", "123456" },
            { "admin", "admin" },
            { "zyj", "123456" },
        };

        StartServer();
        GameLog.Log("LoginServer 已自动启动");
    }

    private void OnEnable()
    {
        OnMessageReceived += HandleMessage;
    }

    private void OnDisable()
    {
        OnMessageReceived -= HandleMessage;
    }

    /// <summary>处理客户端发来的登录请求。</summary>
    private void HandleMessage(string clientId, string message)
    {
        try
        {
            var request = JsonUtility.FromJson<LoginRequest>(message);
            if (request == null || request.type != "login")
            {
                Debug.LogWarning($"收到非登录消息，来自 clientId={clientId}: {message}");
                return;
            }

            LoginResponse response = new LoginResponse();

            if (!accountTable.TryGetValue(request.account, out string correctPassword))
            {
                response.success = false;
                response.reason = "Account does not exist";
            }
            else if (correctPassword != request.password)
            {
                response.success = false;
                response.reason = "Wrong password";
            }
            else
            {
                response.success = true;
                response.reason = "";
                response.playerName = request.account;
                response.token = Guid.NewGuid().ToString();
            }

            string jsonResponse = JsonUtility.ToJson(response);
            SendTo(clientId, jsonResponse);
        }
        catch (Exception e)
        {
            Debug.LogError($"处理登录消息异常: {e.Message}\n原始消息: {message}");
            // 发送通用错误响应
            var errorResponse = new LoginResponse
            {
                success = false,
                reason = "Internal server error"
            };
            SendTo(clientId, JsonUtility.ToJson(errorResponse));
        }
    }
}
