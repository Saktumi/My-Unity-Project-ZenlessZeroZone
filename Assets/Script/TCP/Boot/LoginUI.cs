using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoginUI : MonoBehaviour
{
    [Header("页面")]
    [SerializeField] private GameObject m_SplashScreen;
    [SerializeField] private GameObject m_LoginWindow;
    [SerializeField] private GameObject m_LoadingOverlay;

    [Header("输入与控件")]
    [SerializeField] private TMP_InputField m_AccountInput;
    [SerializeField] private TMP_InputField m_PasswordInput;
    [SerializeField] private Toggle m_AgreementToggle;
    [SerializeField] private Button m_LoginButton;
    [SerializeField] private TMP_Text m_StatusText;

    [Header("网络")]
    [SerializeField] private LoginClient m_Client;

    private string m_pendingAccount;
    private string m_pendingPassword;

    private void Awake()
    {
        m_Client.OnClientStarted += HandleStarted;
        m_Client.OnConnectionFailed += HandleConnectionFailed;
        m_Client.OnLoginFailed += HandleLoginFailed;
        m_Client.OnLoginSuccess += HandleLoginSuccess;
    }

    private void OnDestroy()
    {
        m_Client.OnClientStarted -= HandleStarted;
        m_Client.OnConnectionFailed -= HandleConnectionFailed;
        m_Client.OnLoginFailed -= HandleLoginFailed;
        m_Client.OnLoginSuccess -= HandleLoginSuccess;
    }

    /// <summary>启动页按钮：进入登录窗口。</summary>
    public void ShowLoginWindow()
    {
        m_LoginWindow.SetActive(true);
    }

    /// <summary>登录按钮：校验输入后连接并发起登录。</summary>
    public void OnLoginClicked()
    {
        if (string.IsNullOrEmpty(m_AccountInput.text) || string.IsNullOrEmpty(m_PasswordInput.text))
        {
            m_StatusText.text = "Please enter account and password";
            return;
        }
        if (!m_AgreementToggle.isOn)
        {
            m_StatusText.text = "Please agree to the user agreement first";
            return;
        }

        m_pendingAccount = m_AccountInput.text;
        m_pendingPassword = m_PasswordInput.text;

        m_LoadingOverlay.SetActive(true);
        m_LoginButton.interactable = false;
        m_StatusText.text = "Connecting...";

        if (m_Client.connected)
        {
            SendLoginNow();
        }
        else
        {
            m_Client.StartClient();
        }
    }

    private void HandleStarted()
    {
        m_StatusText.text = "Logging in...";
        SendLoginNow();
    }

    private void SendLoginNow()
    {
        m_Client.SendLogin(m_pendingAccount, m_pendingPassword, "1");
    }

    private void HandleConnectionFailed(string reason)
    {
        m_LoadingOverlay.SetActive(false);
        m_LoginButton.interactable = true;
        m_StatusText.text = "Connection failed: " + reason;
    }

    private void HandleLoginFailed(string reason)
    {
        m_LoadingOverlay.SetActive(false);
        m_LoginButton.interactable = true;
        m_StatusText.text = reason;
    }

    private void HandleLoginSuccess(string playerName, string token)
    {
        m_LoadingOverlay.SetActive(false);
        m_StatusText.text = "Welcome, " + playerName;
        GameLog.Log($"[LoginUI] 登录成功 token={token}，准备切到 Lobby");
        PlayerSession.Set(playerName, token);
        SceneManager.LoadScene("Lobby");
    }
}
