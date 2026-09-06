using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

/// <summary>
/// 基础 TCP 服务器：多客户端连接，
/// 接收线程处理网络，事件统一回主线程触发。
/// </summary>
public class Server : MonoBehaviour
{
    [Header("网络设置")]
    public string ipAddress = "127.0.0.1";
    public int port = 54010;

    protected TcpListener m_listener;
    protected readonly object m_clientsLock = new object();
    protected readonly List<ServerClient> m_clients = new List<ServerClient>();
    protected volatile bool m_running;

    protected readonly ConcurrentQueue<Action> m_mainThreadQueue = new ConcurrentQueue<Action>();

    /// <summary>服务器是否正在运行。</summary>
    public bool serverConnected => m_running;
    public bool isRunning => m_running;

    /// <summary>当前已连接的客户端数量。</summary>
    public int clientCount
    {
        get
        {
            lock (m_clientsLock) return m_clients.Count;
        }
    }

    /// <summary>服务器启动成功时触发。</summary>
    public event Action OnServerStarted;
    /// <summary>服务器关闭时触发。</summary>
    public event Action OnServerStopped;
    /// <summary>有新客户端连接时触发。</summary>
    public event Action OnClientConnected;
    /// <summary>有客户端断开时触发，参数为客户端 ID（端点地址）。</summary>
    public event Action<string> OnClientDisconnected;
    /// <summary>收到客户端消息时触发，参数为客户端 ID 和消息内容。</summary>
    public event Action<string, string> OnMessageReceived;

    protected virtual void Update()
    {
        DispatchQueue();
    }

    protected virtual void OnDestroy()
    {
        CloseServer();
    }

    /// <summary>启动服务器并开始接受客户端。</summary>
    public virtual void StartServer()
    {
        if (m_running)
        {
            GameLog.Log("[Server] 服务器已在运行");
            return;
        }

        try
        {
            IPAddress ip = IPAddress.Parse(ipAddress);
            m_listener = new TcpListener(ip, port);
            m_listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            m_listener.Start();

            m_running = true;
            GameLog.Log($"[Server] 服务器已启动 {ipAddress}:{port}");

            Thread acceptThread = new Thread(AcceptLoop)
            {
                IsBackground = true
            };
            acceptThread.Start();

            OnServerStarted?.Invoke();
        }
        catch (Exception e)
        {
            m_running = false;
            Debug.LogError($"[Server] 服务器启动失败: {e.Message}");
        }
    }

    /// <summary>停止服务器并断开所有客户端。</summary>
    public virtual void CloseServer()
    {
        if (!m_running)
        {
            return;
        }

        m_running = false;
        try { m_listener?.Stop(); } catch (Exception) { }
        m_listener = null;

        lock (m_clientsLock)
        {
            for (int i = m_clients.Count - 1; i >= 0; i--)
            {
                RemoveClient(m_clients[i]);
            }
        }

        OnServerStopped?.Invoke();
    }

    /// <summary>向指定客户端发送消息。</summary>
    public virtual void SendTo(string clientId, string text)
    {
        lock (m_clientsLock)
        {
            foreach (ServerClient client in m_clients)
            {
                if (client.id == clientId)
                {
                    TrySend(client, text);
                    return;
                }
            }
        }
    }

    /// <summary>向所有客户端广播消息。</summary>
    public virtual void Broadcast(string text)
    {
        lock (m_clientsLock)
        {
            foreach (ServerClient client in m_clients)
            {
                TrySend(client, text);
            }
        }
    }

    // ===== 后台线程逻辑 =====

    protected virtual void AcceptLoop()
    {
        while (m_running)
        {
            try
            {
                TcpClient tcpClient = m_listener.AcceptTcpClient();
                ServerClient client = new ServerClient(tcpClient);

                lock (m_clientsLock)
                {
                    m_clients.Add(client);
                }

                GameLog.Log($"[Server] 客户端已连接: {client.id}");

                Thread receiveThread = new Thread(() => ClientReceiveLoop(client))
                {
                    IsBackground = true
                };
                receiveThread.Start();

                Enqueue(() => OnClientConnected?.Invoke());
            }
            catch (Exception e)
            {
                if (m_running)
                {
                    Debug.LogWarning($"[Server] 接受连接异常: {e.Message}");
                }
                break;
            }
        }
    }

    protected virtual void ClientReceiveLoop(ServerClient client)
    {
        byte[] lengthBuffer = new byte[TcpFraming.LengthPrefixSize];
        byte[] payloadBuffer = new byte[TcpFraming.MaxMessageSize];

        while (client.running && m_running)
        {
            try
            {
                string message = TcpFraming.ReadFrame(client.stream, lengthBuffer, payloadBuffer);
                if (message == null)
                {
                    break; // 客户端已关闭
                }

                string clientId = client.id;
                string captured = message;
                Enqueue(() => OnMessageReceived?.Invoke(clientId, captured));
            }
            catch (Exception e)
            {
                if (m_running)
                {
                    Debug.LogWarning($"[Server] 客户端 {client.id} 接收异常: {e.Message}");
                }
                break;
            }
        }

        RemoveClient(client);

        string disconnectedId = client.id;
        Enqueue(() => OnClientDisconnected?.Invoke(disconnectedId));
    }

    protected virtual void RemoveClient(ServerClient client)
    {
        client.running = false;
        try { client.stream?.Close(); } catch (Exception) { }
        try { client.tcp.Close(); } catch (Exception) { }

        lock (m_clientsLock)
        {
            m_clients.Remove(client);
        }
    }

    protected virtual void TrySend(ServerClient client, string text)
    {
        try
        {
            byte[] frame = TcpFraming.Encode(text);
            client.stream.Write(frame, 0, frame.Length);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Server] 发送给 {client.id} 失败: {e.Message}");
            RemoveClient(client);
        }
    }

    // ===== 主线程队列 =====

    protected void Enqueue(Action action) => m_mainThreadQueue.Enqueue(action);

    protected virtual void DispatchQueue()
    {
        while (m_mainThreadQueue.TryDequeue(out Action action))
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    /// <summary>服务器持有的单个客户端连接。</summary>
    protected class ServerClient
    {
        public readonly TcpClient tcp;
        public readonly NetworkStream stream;
        public readonly string id;
        public volatile bool running = true;

        public ServerClient(TcpClient tcp)
        {
            this.tcp = tcp;
            stream = tcp.GetStream();
            id = tcp.Client.RemoteEndPoint?.ToString() ?? "unknown";
        }
    }
}
