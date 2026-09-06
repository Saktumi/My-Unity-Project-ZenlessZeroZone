using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 破防 QTE 控制器：负责破防 → 时停选人 → 左右候选 → 连携触发的完整时序。
/// 配置由 CharacterSwitcher 注入，选人结果通过事件交还其执行切人。
/// </summary>
public class QteFlow : MonoBehaviour
{
    /// <summary>选人完成：参数为成员下标与破防敌人。</summary>
    public event System.Action<int, Enemy> AssistSelected;

    public bool IsActive => m_active;

    /// <summary>当前 QTE 锁定的破防敌人。</summary>
    public Enemy currentEnemy => m_enemy;

    protected bool m_active;
    protected bool m_accepting;
    protected Enemy m_enemy;
    protected float m_remaining;
    protected int m_leftIndex = -1;
    protected int m_rightIndex = -1;
    protected Coroutine m_timeline;
    protected SwitchTimePanel m_panel;
    protected PlayerCamera m_cameraRig;

    // ===== 配置快照（由 CharacterSwitcher.SetupBattle 注入） =====
    protected bool m_useQteCamera = true;
    protected float m_zoomScale = 0.8f;
    protected float m_cameraHold = 0.3f;
    protected float m_slowMotionScale = 0.06f;
    protected float m_slowMotionHold = 0.2f;
    protected float m_windowTime = 3f;
    protected bool m_verboseLog = true;
    protected System.Func<int> m_countProvider;
    protected System.Func<int> m_activeIndexProvider;
    protected System.Func<int, string> m_nameProvider;
    protected System.Func<int, Color> m_colorProvider;

    protected InputAction m_leftAction;
    protected InputAction m_rightAction;

    protected virtual void Awake()
    {
        CreateActions();
    }

    protected virtual void OnDisable()
    {
        CancelQte(false);
        m_leftAction?.Disable();
        m_rightAction?.Disable();
    }

    protected virtual void Update()
    {
        if (!m_active || !m_accepting)
        {
            return;
        }

        m_remaining -= Time.unscaledDeltaTime;
        m_panel?.SetRemaining(m_remaining);

        if (m_remaining <= 0f)
        {
            CancelQte(false);
            return;
        }

        if (m_leftAction != null && m_leftAction.WasPressedThisFrame())
        {
            ExecuteAssist(m_leftIndex);
            return;
        }
        if (m_rightAction != null && m_rightAction.WasPressedThisFrame())
        {
            ExecuteAssist(m_rightIndex);
        }
    }

    /// <summary>注入 QTE 配置与成员查询委托。</summary>
    public virtual void Configure(bool useQteCamera, float zoomScale, float cameraHold,
        float slowMotionScale, float slowMotionHold, float windowTime, bool verboseLog,
        System.Func<int> countProvider, System.Func<int> activeIndexProvider,
        System.Func<int, string> nameProvider, System.Func<int, Color> colorProvider)
    {
        m_useQteCamera = useQteCamera;
        m_zoomScale = zoomScale;
        m_cameraHold = cameraHold;
        m_slowMotionScale = slowMotionScale;
        m_slowMotionHold = slowMotionHold;
        m_windowTime = windowTime;
        m_verboseLog = verboseLog;
        m_countProvider = countProvider;
        m_activeIndexProvider = activeIndexProvider;
        m_nameProvider = nameProvider;
        m_colorProvider = colorProvider;
    }

    /// <summary>敌人破防后开启 QTE 选人窗口。</summary>
    public virtual void StartQte(Enemy enemy)
    {
        if (enemy == null || m_active)
        {
            return;
        }

        m_active = true;
        m_accepting = false;
        m_enemy = enemy;
        m_remaining = 0f;

        // 冻结当前玩家输入，可选轻微拉近镜头
        var activePlayer = CharacterSwitcher.instance != null ? CharacterSwitcher.instance.activePlayer : null;
        activePlayer?.inputs?.actions?.Disable();
        if (m_useQteCamera)
        {
            ApplyZoom();
        }
        CameraShakeManager.instance?.Shake(0.55f, 0.2f);

        if (m_timeline != null)
        {
            StopCoroutine(m_timeline);
        }
        m_timeline = StartCoroutine(QteTimeline(enemy));
        Log("[QteFlow] 敌人破防，QTE 选人窗口开启");
    }

    /// <summary>QTE 时间轴：镜头拉近 → 慢动作 → 弹出选人 UI。</summary>
    protected virtual IEnumerator QteTimeline(Enemy enemy)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, m_cameraHold));
        if (!m_active || enemy == null || enemy.isDead)
        {
            CancelQte(false);
            yield break;
        }

        // 慢动作时长需覆盖整个选人窗口
        HitFeelManager.instance?.SlowMotion(m_slowMotionScale,
            m_windowTime + m_cameraHold + m_slowMotionHold + 1.2f);

        yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, m_slowMotionHold));
        if (!m_active || enemy == null || enemy.isDead)
        {
            CancelQte(false);
            yield break;
        }

        int activeIndex = m_activeIndexProvider != null ? m_activeIndexProvider() : -1;
        m_leftIndex = NextMemberIndex(activeIndex);
        // 只有两名成员时左右候选相同
        m_rightIndex = MemberCount() > 2 ? NextMemberIndex(m_leftIndex) : m_leftIndex;
        m_remaining = m_windowTime;

        EnsurePanel();
        if (m_panel != null)
        {
            m_panel.Show(GetMemberName(m_leftIndex), GetMemberColor(m_leftIndex),
                GetMemberName(m_rightIndex), GetMemberColor(m_rightIndex), m_windowTime);
        }
        m_leftAction?.Enable();
        m_rightAction?.Enable();
        m_accepting = true;
    }

    /// <summary>执行连携：收尾 QTE 后通知切到目标成员。</summary>
    public virtual void ExecuteAssist(int index)
    {
        if (index < 0 || index >= MemberCount())
        {
            CancelQte(false);
            return;
        }

        Enemy assistEnemy = m_enemy;
        CancelQte(false);
        if (assistEnemy == null || assistEnemy.isDead)
        {
            return;
        }

        AssistSelected?.Invoke(index, assistEnemy);
    }

    public virtual void CancelQte(bool verbose)
    {
        if (!m_active)
        {
            return;
        }

        if (m_timeline != null)
        {
            StopCoroutine(m_timeline);
            m_timeline = null;
        }
        m_active = false;
        m_accepting = false;
        m_enemy = null;
        m_leftIndex = -1;
        m_rightIndex = -1;
        m_leftAction?.Disable();
        m_rightAction?.Disable();
        m_panel?.Hide();
        RestoreZoom();
        var activePlayer = CharacterSwitcher.instance != null ? CharacterSwitcher.instance.activePlayer : null;
        activePlayer?.inputs?.actions?.Enable();
        HitFeelManager.instance?.RestoreNow();
        if (verbose)
        {
            Log("[QteFlow] QTE 取消");
        }
    }

    protected virtual void CreateActions()
    {
        if (m_leftAction == null)
        {
            m_leftAction = new InputAction("QtePickLeft", InputActionType.Button, "<Mouse>/leftButton");
        }
        if (m_rightAction == null)
        {
            m_rightAction = new InputAction("QtePickRight", InputActionType.Button, "<Mouse>/rightButton");
        }
    }

    protected virtual int MemberCount()
    {
        return m_countProvider != null ? m_countProvider() : 0;
    }

    protected virtual int NextMemberIndex(int from)
    {
        int count = MemberCount();
        if (count <= 0)
        {
            return -1;
        }
        if (from < 0)
        {
            return 0;
        }
        return (from + 1) % count;
    }

    protected virtual string GetMemberName(int index)
    {
        if (m_nameProvider != null)
        {
            var name = m_nameProvider(index);
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }
        }
        return "-";
    }

    protected virtual Color GetMemberColor(int index)
    {
        if (m_colorProvider != null)
        {
            return m_colorProvider(index);
        }
        return Color.white;
    }

    protected virtual void EnsurePanel()
    {
        if (m_panel == null)
        {
            m_panel = GetComponent<SwitchTimePanel>();
        }
        if (m_panel == null)
        {
            // 面板可能挂在场景其它物体上
            m_panel = Object.FindFirstObjectByType<SwitchTimePanel>();
        }
        if (m_panel == null)
        {
            m_panel = gameObject.AddComponent<SwitchTimePanel>();
        }
    }

    protected virtual void ApplyZoom()
    {
        if (m_cameraRig == null)
        {
            m_cameraRig = Object.FindFirstObjectByType<PlayerCamera>();
        }
        m_cameraRig?.SetCameraZoomScale(m_zoomScale);
    }

    protected virtual void RestoreZoom()
    {
        m_cameraRig?.ResetCameraZoomScale();
    }

    protected virtual void Log(string message)
    {
        if (m_verboseLog)
        {
            GameLog.Log(message);
        }
    }
}
