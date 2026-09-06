using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 基础 TCP 客户端：网络收发在后台线程执行，
/// 事件统一回 Unity 主线程触发。
/// </summary>
public class Client : MonoBehaviour
{
    [Header("网络设置")]
    public string ipAddress = "127.0.0.1";
    public int port = 54010;

    [Min(0.5f)]
    public float connectTimeout = 5f;

    protected TcpClient m_client;
    protected NetworkStream m_stream;
    protected volatile bool m_running;

    protected readonly ConcurrentQueue<Action> m_mainThreadQueue = new ConcurrentQueue<Action>();

    public bool connected => m_client != null && m_client.Connected;

    /// <summary>连接成功时触发。</summary>
    public event Action OnClientStarted;
    /// <summary>连接失败时触发，参数为失败原因。</summary>
    public event Action<string> OnConnectionFailed;
    /// <summary>连接断开时触发（网络异常或服务器关闭）。主动 StopClient 不触发。</summary>
    public event Action OnDisconnected;
    /// <summary>收到服务器消息时触发。</summary>
    public event Action<string> OnMessageReceived;

    // 子类（登录 / 聊天客户端）通过覆写本方法完成事件订阅
    protected virtual void Awake()
    {
    }

    protected virtual void Update()
    {
        DispatchQueue();
    }

    protected virtual void OnDestroy()
    {
        StopClient();
    }

    /// <summary>连接到服务器（异步，不阻塞主线程）。</summary>
    public virtual void StartClient()
    {
        if (m_running)
        {
            GameLog.Log("[Client] 正在连接或已连接，请勿重复操作");
            return;
        }

        m_running = true;
        m_client = new TcpClient();

        Thread thread = new Thread(ConnectThread)
        {
            IsBackground = true
        };
        thread.Start();
    }

    /// <summary>发送一条消息（自动加长度前缀）。</summary>
    public virtual void Send(string text)
    {
        if (!connected)
        {
            Debug.LogWarning("[Client] 未连接，无法发送消息");
            return;
        }

        try
        {
            byte[] frame = TcpFraming.Encode(text);
            m_stream.Write(frame, 0, frame.Length);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Client] 发送失败: {e.Message}");
        }
    }

    /// <summary>主动断开连接。</summary>
    public virtual void StopClient()
    {
        m_running = false;
        CloseSockets();
    }

    // ===== 后台线程逻辑 =====

    protected virtual void ConnectThread()
    {
        try
        {
            Task connectTask = m_client.ConnectAsync(ipAddress, port);
            if (!connectTask.Wait(TimeSpan.FromSeconds(connectTimeout)))
            {
                m_running = false;
                CloseSockets();
                Enqueue(() => OnConnectionFailed?.Invoke("Connection timed out"));
                return;
            }

            m_stream = m_client.GetStream();
            Enqueue(() => OnClientStarted?.Invoke());

            ReceiveLoop();
        }
        catch (AggregateException e)
        {
            // 解包 ConnectAsync 的 AggregateException
            Exception inner = e.InnerException ?? e;
            m_running = false;
            CloseSockets();
            Enqueue(() => OnConnectionFailed?.Invoke(inner.Message));
        }
        catch (Exception e)
        {
            m_running = false;
            CloseSockets();
            Enqueue(() => OnConnectionFailed?.Invoke(e.Message));
        }
    }

    protected virtual void ReceiveLoop()
    {
        byte[] lengthBuffer = new byte[TcpFraming.LengthPrefixSize];
        byte[] payloadBuffer = new byte[TcpFraming.MaxMessageSize];

        while (m_running)
        {
            try
            {
                string message = TcpFraming.ReadFrame(m_stream, lengthBuffer, payloadBuffer);
                if (message == null)
                {
                    break; // 服务器已关闭
                }

                string captured = message;
                Enqueue(() => OnMessageReceived?.Invoke(captured));
            }
            catch (Exception e)
            {
                if (m_running)
                {
                    Debug.LogWarning($"[Client] 接收异常: {e.Message}");
                }
                break;
            }
        }

        if (m_running)
        {
            // 非主动断开时通知 UI
            m_running = false;
            CloseSockets();
            Enqueue(() => OnDisconnected?.Invoke());
        }
    }

    protected void CloseSockets()
    {
        try { m_stream?.Close(); } catch (Exception) { }
        try { m_client?.Close(); } catch (Exception) { }
        m_stream = null;
        m_client = null;
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
}
