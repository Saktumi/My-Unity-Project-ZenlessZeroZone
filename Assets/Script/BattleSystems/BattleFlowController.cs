using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 战斗流程收尾：全员敌人被击破后结算胜利，
/// 当前成员阵亡后结算失败，并提供重开 / 返回大厅。
/// </summary>
public class BattleFlowController : MonoBehaviour
{
    public enum BattleResult
    {
        None,
        Victory,
        Defeat,
    }

    public BattleResult result => m_result;

    [Tooltip("胜利判定延迟（秒），等待死亡演出播完")]
    public float victoryDelay = 1.0f;
    [Tooltip("失败判定延迟（秒）")]
    public float defeatDelay = 0.6f;

    protected BattleResult m_result = BattleResult.None;
    protected bool m_ended;
    protected int m_startAliveCount = -1;
    protected float m_pendingDelay = -1f;
    protected BattleResult m_pendingResult = BattleResult.None;
    protected Font m_sourceFont;
    protected bool m_panelShown;
    protected RectTransform m_retryButtonRect;
    protected RectTransform m_lobbyButtonRect;

    protected virtual void OnDestroy()
    {
        if (Time.timeScale <= 0f)
        {
            Time.timeScale = 1f;
        }
    }

    /// <summary>BattleBootstrap 注入中文字体源（萝莉体）。</summary>
    public virtual void Setup(Font sourceFont)
    {
        m_sourceFont = sourceFont;
    }

    protected virtual void Update()
    {
        // 结算面板出现后优先处理按钮输入
        if (m_panelShown)
        {
            HandleResultPanelInput();
            return;
        }

        if (m_ended)
        {
            return;
        }

        // 首帧记录初始存活敌人
        if (m_startAliveCount < 0)
        {
            int alive = CountRegisteredAliveEnemies();
            if (alive > 0)
            {
                m_startAliveCount = alive;
            }
            return;
        }

        var player = ResolveActivePlayer();
        if (player != null && player.currentHp <= 0f && !player.IsInvincible)
        {
            BeginResult(BattleResult.Defeat, defeatDelay);
            return;
        }

        if (CountRegisteredAliveEnemies() == 0)
        {
            BeginResult(BattleResult.Victory, victoryDelay);
        }
    }

    protected virtual void BeginResult(BattleResult next, float delay)
    {
        if (m_ended || next == BattleResult.None)
        {
            return;
        }

        m_ended = true;
        m_result = next;

        if (next == BattleResult.Defeat)
        {
            // 失败立即冻结，避免结算前继续操作
            Time.timeScale = 0f;
            LevelManager.LockCursor(false);
        }

        m_pendingResult = next;
        m_pendingDelay = Mathf.Max(0f, delay);
        StartCoroutine(PendingShowRoutine());
    }

    protected virtual IEnumerator PendingShowRoutine()
    {
        yield return new WaitForSecondsRealtime(m_pendingDelay);
        ShowResultPanel(m_pendingResult);
    }

    /// <summary>弹出结果面板并锁定流程（胜利 / 失败都冻结 timeScale）。</summary>
    protected virtual void ShowResultPanel(BattleResult final)
    {
        if (m_panelShown)
        {
            return;
        }
        m_panelShown = true;

        Time.timeScale = 0f;
        LevelManager.LockCursor(false);

        var pauser = LevelPauser.instance;
        if (pauser != null)
        {
            pauser.canPause = false;
            if (pauser.paused)
            {
                pauser.Pause(false);
                LevelManager.LockCursor(false);
            }
        }

        BuildPanel(final);
    }

    protected virtual int CountRegisteredAliveEnemies()
    {
        int alive = 0;
        for (int i = 0; i < EnemyRegistry.Count; i++)
        {
            var enemy = EnemyRegistry.GetAt(i);
            if (enemy != null && enemy.gameObject != null && !enemy.isDead)
            {
                alive++;
            }
        }
        return alive;
    }

    protected virtual Player ResolveActivePlayer()
    {
        var switcher = CharacterSwitcher.instance;
        if (switcher != null && switcher.activePlayer != null)
        {
            return switcher.activePlayer;
        }
        for (int i = 0; i < PlayerRegistry.Count; i++)
        {
            var player = PlayerRegistry.GetAt(i);
            if (player != null && player.gameObject != null && player.gameObject.activeInHierarchy)
            {
                return player;
            }
        }
        return null;
    }

    // ===== 结果面板（纯代码构建，字体来自 BattleBootstrap） =====

    protected virtual void BuildPanel(BattleResult final)
    {
        // 独立顶层画布，保证结算按钮可点击
        EnsureEventSystem();
        var canvas = EnsureResultCanvas();

        var root = new GameObject("BattleResultPanel", typeof(RectTransform),
            typeof(CanvasGroup), typeof(Image));
        var rootRect = (RectTransform)root.transform;
        rootRect.SetParent(canvas.transform, false);
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        // 背景只做遮罩，不拦截按钮点击
        var backdrop = root.GetComponent<Image>();
        backdrop.color = new Color(0.02f, 0.02f, 0.05f, 0.78f);
        backdrop.raycastTarget = false;

        var group = root.GetComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;

        string title = final == BattleResult.Victory ? "讨伐完成" : "作战失败";
        string subtitle = final == BattleResult.Victory
            ? "敌人已全部击破"
            : "当前代理人倒下了";

        var centerGo = new GameObject("Center", typeof(RectTransform));
        var center = (RectTransform)centerGo.transform;
        center.SetParent(rootRect, false);
        center.anchorMin = new Vector2(0.5f, 0.55f);
        center.anchorMax = new Vector2(0.5f, 0.55f);
        center.pivot = new Vector2(0.5f, 0.5f);
        center.sizeDelta = new Vector2(480f, 260f);

        CreateLabel(center, "Title", title,
            final == BattleResult.Victory ? new Color(1f, 0.86f, 0.38f) : new Color(1f, 0.42f, 0.36f),
            46, new Vector2(0f, 70f));
        CreateLabel(center, "Subtitle", subtitle, new Color(0.82f, 0.84f, 0.9f),
            22, new Vector2(0f, 26f));

        float buttonY = -46f;
        m_retryButtonRect = CreateButton(center, "RetryButton", "再战一场",
            new Vector2(-118f, buttonY), new Vector2(210f, 54f), () => RestartBattle());
        m_lobbyButtonRect = CreateButton(center, "LobbyButton", "返回大厅",
            new Vector2(118f, buttonY), new Vector2(210f, 54f), () => BackToLobby());
    }

    /// <summary>结算按钮输入：鼠标按下 + 矩形命中判定触发。</summary>
    protected virtual void HandleResultPanelInput()
    {
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
        {
            return;
        }

        Vector2 screen = mouse.position.ReadValue();
        if (m_retryButtonRect != null
            && RectTransformUtility.RectangleContainsScreenPoint(m_retryButtonRect, screen, null))
        {
            m_panelShown = false;
            RestartBattle();
            return;
        }

        if (m_lobbyButtonRect != null
            && RectTransformUtility.RectangleContainsScreenPoint(m_lobbyButtonRect, screen, null))
        {
            m_panelShown = false;
            BackToLobby();
        }
    }

    /// <summary>创建结算面板专用画布，层级高于战斗 HUD。</summary>
    protected virtual Canvas EnsureResultCanvas()
    {
        var go = new GameObject("BattleResultCanvas", typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(transform, false);

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;
        canvas.pixelPerfect = false;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        scaler.referencePixelsPerUnit = 100f;
        return canvas;
    }

    /// <summary>场景缺少 EventSystem 时自动补一个，保证运行时创建的按钮可交互。</summary>
    protected virtual void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        var go = new GameObject("EventSystem", typeof(EventSystem),
            typeof(InputSystemUIInputModule));
    }

    protected virtual Text CreateLabel(RectTransform parent, string nodeName, string content,
        Color color, int fontSize, Vector2 anchoredPosition)
    {
        var go = new GameObject(nodeName, typeof(RectTransform), typeof(Text));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(520f, 60f);
        rect.anchoredPosition = anchoredPosition;

        var text = go.GetComponent<Text>();
        text.text = content;
        text.color = color;
        text.fontSize = fontSize;
        text.font = ResolveFont();
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    protected virtual RectTransform CreateButton(RectTransform parent, string nodeName, string label,
        Vector2 anchoredPosition, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(nodeName, typeof(RectTransform), typeof(Image), typeof(Button));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        var image = go.GetComponent<Image>();
        image.color = new Color(0.12f, 0.14f, 0.2f, 0.95f);
        image.raycastTarget = true;

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.interactable = true;
        var colors = button.colors;
        colors.highlightedColor = new Color(0.26f, 0.3f, 0.42f, 1f);
        colors.pressedColor = new Color(0.05f, 0.06f, 0.1f, 1f);
        button.colors = colors;
        button.onClick.AddListener(onClick);

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
        var labelRect = (RectTransform)labelGo.transform;
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var text = labelGo.GetComponent<Text>();
        text.text = label;
        text.color = Color.white;
        text.fontSize = 24;
        text.font = ResolveFont();
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return rect;
    }

    protected virtual Font ResolveFont()
    {
        if (m_sourceFont != null)
        {
            return m_sourceFont;
        }
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        return font;
    }

    protected virtual void RestartBattle()
    {
        Time.timeScale = 1f;
        LevelManager.LockCursor(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    protected virtual void BackToLobby()
    {
        Time.timeScale = 1f;
        LevelManager.LockCursor(false);
        SceneManager.LoadScene("Lobby");
    }
}
