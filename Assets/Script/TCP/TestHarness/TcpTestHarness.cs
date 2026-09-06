using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TCP auto-test harness (belongs to the TestHarness test scene only).
/// Drives the Server / Client base components through a scripted verification:
///   1. TcpFraming encode/decode (empty, Chinese, and large messages)
///   2. Client connects to server
///   3. Client sends multiple frames -> server echoes each -> client receives them (verifies packet fragmentation)
///   4. Multiple clients + Broadcast
///   5. Disconnect notifications and server shutdown
/// Does not modify any existing network code; all events fire on the Unity main thread.
/// </summary>
public class TcpTestHarness : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button m_AutoTestButton = null;
    [SerializeField] private TMP_Text m_ResultText = null;

    private bool m_testRunning;
    private string m_failReason;

    private void Awake()
    {
        if (m_AutoTestButton != null)
        {
            m_AutoTestButton.onClick.AddListener(RunAutoTest);
        }
        SetResult("Click Run Auto Test to verify the TCP framework");
    }

    public void RunAutoTest()
    {
        if (m_testRunning)
        {
            SetResult("Auto test is already running, please wait...");
            return;
        }

        m_testRunning = true;
        StartCoroutine(AutoTestFlow());
    }

    private IEnumerator AutoTestFlow()
    {
        SetResult("Running auto test...");
        m_failReason = null;

        // Warn first if the manual server occupies the port, so a failed run is not misleading
        CustomServer manualServer = FindFirstObjectByType<CustomServer>();
        if (manualServer != null && manualServer.isRunning)
        {
            Fail("Please stop the manual server first (port 54010 is in use)");
            m_testRunning = false;
            yield break;
        }

        // ---------- Step 0: direct framing tool verification ----------
        string framingError = null;
        try
        {
            VerifyFramingRoundTrip("");
            VerifyFramingRoundTrip("hello tcp");
            VerifyFramingRoundTrip("你好，绝区零！"); // Chinese payload: verifies UTF-8 encoding
            VerifyFramingRoundTrip("😀 multibyte emoji test");
            VerifyFramingRoundTrip(new string('长', 10000)); // ~30KB, verifies large messages
        }
        catch (Exception e)
        {
            framingError = e.Message;
        }

        if (framingError != null)
        {
            Fail("Framing encode/decode test failed: " + framingError);
            m_testRunning = false;
            yield break;
        }

        // ---------- Prepare server and clients (try/finally guarantees cleanup; coroutines cannot yield inside a try with a catch) ----------
        GameObject serverGo = null;
        GameObject clientGo = null;
        GameObject client2Go = null;

        try
        {
            serverGo = new GameObject("AutoTest Server");
            Server server = serverGo.AddComponent<Server>();
            server.ipAddress = "127.0.0.1";
            server.port = 54010;

            clientGo = new GameObject("AutoTest Client");
            Client client = clientGo.AddComponent<Client>();
            client.ipAddress = "127.0.0.1";
            client.port = 54010;
            client.connectTimeout = 5f;

            List<string> receivedByClient1 = new List<string>();
            client.OnMessageReceived += receivedByClient1.Add;
            server.OnMessageReceived += (id, msg) => server.SendTo(id, "echo:" + msg); // echo

            // ---------- Step 1: start server and connect client ----------
            server.StartServer();
            yield return WaitUntil(() => server.isRunning, 5f, "Server start timed out");
            if (HasFailed()) { Fail(m_failReason); yield break; }

            client.StartClient();
            yield return WaitUntil(() => client.connected, 10f, "Client connection timed out");
            if (HasFailed()) { Fail(m_failReason); yield break; }

            // ---------- Step 2: send multiple frames rapidly and wait for echoes ----------
            string[] messages =
            {
                "hello tcp",
                "你好，绝区零！",
                "",
                "packet fragment test A|B|C|D",
                "😀 emoji test",
                new string('长', 10000) // ~30KB large message
            };

            foreach (string msg in messages)
            {
                client.Send(msg);
            }

            yield return WaitUntil(() => receivedByClient1.Count >= messages.Length,
                10f, "Timed out waiting for server echo (possible packet fragmentation issue)");
            if (HasFailed()) { Fail(m_failReason); yield break; }

            for (int i = 0; i < messages.Length; i++)
            {
                string expect = "echo:" + messages[i];
                if (receivedByClient1[i] != expect)
                {
                    Fail($"Echo mismatch: expected [{expect}] got [{receivedByClient1[i]}]");
                    yield break;
                }
            }

            // ---------- Step 3: second client + broadcast ----------
            client2Go = new GameObject("AutoTest Client2");
            Client client2 = client2Go.AddComponent<Client>();
            client2.ipAddress = "127.0.0.1";
            client2.port = 54010;
            client2.connectTimeout = 5f;
            List<string> receivedByClient2 = new List<string>();
            client2.OnMessageReceived += receivedByClient2.Add;

            client2.StartClient();
            yield return WaitUntil(() => client2.connected, 10f, "Second client connection timed out");
            if (HasFailed()) { Fail(m_failReason); yield break; }
            yield return WaitUntil(() => server.clientCount >= 2, 5f, "Server did not register the second client");
            if (HasFailed()) { Fail(m_failReason); yield break; }

            const string broadcastMsg = "broadcast-to-all";
            server.Broadcast(broadcastMsg);
            yield return WaitUntil(
                () => receivedByClient1.Contains(broadcastMsg) && receivedByClient2.Contains(broadcastMsg),
                5f, "Broadcast did not reach all clients");
            if (HasFailed()) { Fail(m_failReason); yield break; }

            // ---------- Step 4: disconnect notification and server shutdown ----------
            bool client1Disconnected = false;
            bool serverStopped = false;
            client.OnDisconnected += () => client1Disconnected = true;
            server.OnServerStopped += () => serverStopped = true;

            client2.StopClient();
            yield return WaitUntil(() => server.clientCount == 1, 5f, "Server did not notice client 2 disconnect");
            if (HasFailed()) { Fail(m_failReason); yield break; }

            server.CloseServer();
            yield return WaitUntil(() => server.clientCount == 0, 5f, "Server did not clean up connections after closing");
            if (HasFailed()) { Fail(m_failReason); yield break; }
            yield return WaitUntil(() => client1Disconnected && serverStopped, 5f, "Did not receive disconnect/close events");
            if (HasFailed()) { Fail(m_failReason); yield break; }

            SetResult(
                $"✅ All tests passed: framing✓ connect✓ echo {messages.Length}✓ broadcast✓ disconnect✓ (127.0.0.1:54010)");
            Debug.Log("[TcpTestHarness] TCP auto test passed, echo count: " + messages.Length);
        }
        finally
        {
            if (serverGo != null) Destroy(serverGo);
            if (clientGo != null) Destroy(clientGo);
            if (client2Go != null) Destroy(client2Go);
            m_testRunning = false;
        }
    }

    /// <summary>Encodes the string into a frame, then reads it back from a stream to verify TcpFraming round-trips correctly.</summary>
    private static void VerifyFramingRoundTrip(string text)
    {
        byte[] frame = TcpFraming.Encode(text);
        using (MemoryStream stream = new MemoryStream(frame))
        {
            string decoded = TcpFraming.ReadFrame(
                stream,
                new byte[TcpFraming.LengthPrefixSize],
                new byte[TcpFraming.MaxMessageSize]);

            if (decoded != text)
            {
                throw new InvalidOperationException($"Encoding/decoding mismatch: [{text}] -> [{decoded}]");
            }
        }
    }

    private IEnumerator WaitUntil(Func<bool> condition, float timeout, string timeoutMessage)
    {
        float start = Time.realtimeSinceStartup;
        while (!condition())
        {
            if (Time.realtimeSinceStartup - start > timeout)
            {
                m_failReason = timeoutMessage;
                yield break;
            }
            yield return null;
        }
    }

    private bool HasFailed()
    {
        return !string.IsNullOrEmpty(m_failReason);
    }

    private void Fail(string message)
    {
        m_failReason = message;
        SetResult("❌ Auto test failed: " + message);
        Debug.LogError("[TcpTestHarness] " + message);
    }

    private void SetResult(string text)
    {
        if (m_ResultText != null)
        {
            m_ResultText.text = text;
        }
        Debug.Log("[TcpTestHarness] " + text);
    }
}
