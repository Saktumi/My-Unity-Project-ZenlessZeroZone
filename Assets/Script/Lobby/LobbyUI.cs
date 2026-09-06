using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 大厅主控制：玩家名与图鉴进度显示、功能窗口切换，
/// 并将场景 TMP 文本切换为运行时动态生成的中文字体。
/// </summary>
public class LobbyUI : MonoBehaviour
{
    public static LobbyUI instance { get; private set; }

    /// <summary>运行时动态中文字体。</summary>
    public static TMP_FontAsset lobbyFont { get; private set; }

    /// <summary>中文 ttf 源字体，供 legacy Text 渲染。</summary>
    public static Font chineseSourceFont { get; private set; }

    [Header("中文字体源（萝莉体.ttf，运行时生成动态 TMP 字体）")]
    [SerializeField] private Font sourceFont;

    private GameObject m_codexWindow;
    private GameObject m_gachaWindow;
    private GameObject m_battleWindow;
    private GameObject m_chatWindow;

    private Text m_playerName;
    private Text m_progressText;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        chineseSourceFont = sourceFont;

        // 用 transform.Find 缓存隐藏窗口
        m_codexWindow = transform.Find("CodexWindow")?.gameObject;
        m_gachaWindow = transform.Find("GachaWindow")?.gameObject;
        m_battleWindow = transform.Find("BattleWindow")?.gameObject;
        m_chatWindow = transform.Find("ChatWindow")?.gameObject;

        // 默认只显示主界面，窗口由按钮打开
        if (m_codexWindow != null)
        {
            m_codexWindow.SetActive(false);
        }
        if (m_gachaWindow != null)
        {
            m_gachaWindow.SetActive(false);
        }
        if (m_battleWindow != null)
        {
            m_battleWindow.SetActive(false);
        }
        if (m_chatWindow != null)
        {
            m_chatWindow.SetActive(false);
        }
    }

    private IEnumerator Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        yield return null;

        m_playerName = FindHeaderText("MainScreen/TopBar/PlayerName");
        m_progressText = FindHeaderText("MainScreen/TopBar/ProgressChip/ProgressText");

        ApplyDynamicChineseFont();
        RefreshHeader();

        var codex = m_codexWindow != null ? m_codexWindow.GetComponent<CodexPanel>() : null;
        codex?.Refresh();
    }

    /// <summary>按路径查找 Text，优先取物体自身。</summary>
    private Text FindHeaderText(string path)
    {
        var node = transform.Find(path);
        if (node == null)
        {
            return null;
        }
        var text = node.GetComponent<Text>();
        return text != null ? text : node.GetComponentInChildren<Text>(true);
    }

    /// <summary>把 TMP 文本切换为动态中文字体。</summary>
    private void ApplyDynamicChineseFont()
    {
        if (sourceFont == null)
        {
            Debug.LogWarning("[LobbyUI] 未配置中文字体源，界面中文可能无法显示");
            return;
        }

        try
        {
            lobbyFont = TMP_FontAsset.CreateFontAsset(sourceFont);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LobbyUI] 创建动态字体失败: {e.Message}");
            return;
        }
        if (lobbyFont == null)
        {
            Debug.LogError("[LobbyUI] 动态字体创建失败（返回空），请检查萝莉体.ttf 的导入设置");
            return;
        }

        // TMP 输入框使用动态字体以支持中文
        var allInputs = GetComponentsInChildren<TMP_InputField>(true);
        foreach (var input in allInputs)
        {
            if (input.textComponent != null)
            {
                input.textComponent.font = lobbyFont;
            }
            if (input.placeholder is TMPro.TMP_Text placeholderText)
            {
                placeholderText.font = lobbyFont;
            }
            input.fontAsset = lobbyFont;
        }
    }

    public void RefreshHeader()
    {
        if (m_playerName != null)
        {
            m_playerName.text = $"代理人：{PlayerSession.username}";
        }
        RefreshProgress();
    }

    /// <summary>刷新顶栏图鉴进度。</summary>
    public void RefreshProgress()
    {
        if (m_progressText != null)
        {
            m_progressText.text = $"图鉴 {PlayerInventory.Count}/{CharacterCatalog.all.Count}";
        }
    }

    // ===== 窗口切换（场景按钮绑定） =====

    public void ShowCodex()
    {
        m_codexWindow?.GetComponent<CodexPanel>()?.Show();
    }

    public void ShowGacha()
    {
        m_gachaWindow?.GetComponent<GachaPanel>()?.Show();
    }

    public void ShowBattle()
    {
        m_battleWindow?.GetComponent<BattlePanel>()?.Show();
    }

    public void ShowChat()
    {
        m_chatWindow?.GetComponent<ChatPanel>()?.Show();
    }

    public void BackToLogin()
    {
        PlayerSession.Reset();
        SceneManager.LoadScene("Boot");
    }

}
