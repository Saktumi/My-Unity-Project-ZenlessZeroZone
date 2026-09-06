using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 角色切换器：管理战斗中可切换的玩家成员，
/// 负责 R 键切人、破防 QTE 选人、连携技入场与相机交接。
/// </summary>
public class CharacterSwitcher : MonoBehaviour
{
    public static CharacterSwitcher instance { get; protected set; }

    [System.Serializable]
    public class TeamMember
    {
        [Tooltip("显示名")]
        public string displayName = "AGENT";
        public Player player;
        [Tooltip("相机跟随点；留空自动使用 player.transform 或同名 viewpoint")]
        public Transform followAnchor;
        [Tooltip("染色 / 头像底色")]
        public Color accent = Color.white;
    }

    [Header("成员来源")]
    [Tooltip("优先使用场景中摆放好的角色")]
    public bool useSceneMembers = true;
    [Tooltip("运行时克隆当前角色组成队伍")]
    public bool autoCloneTeam = false;
    public int autoTeamSize = 3;
    public string[] demoMemberNames = { "ANBY", "BILLY", "CORIN" };
    public Color[] demoMemberColors =
    {
        new Color(0.72f, 0.82f, 1f),
        new Color(1f, 0.62f, 0.78f),
        new Color(0.95f, 0.72f, 0.45f),
    };
    [Tooltip("切入出生点距当前角色身后的距离")]
    public float switchSpawnDistance = 0.6f;
    [Header("切入落地修正")]
    [Tooltip("切入期间由代码接管位移，精确落位到旧角色位置")]
    public bool landSwitchInOnOldPosition = true;
    [Tooltip("切入期间向旧角色站位移动的速度")]
    public float switchInLandSpeed = 2.6f;
    [Tooltip("切入落位等待时长（秒）")]
    public float switchInHoldTime = 0.9f;

    [Header("切人动画")]
    [Tooltip("切入动画状态名")]
    public string switchInAnimation = "SwitchIn";
    [Tooltip("切出动画状态名")]
    public string switchOutAnimation = "SwitchOut";
    [Tooltip("切人缓冲：多久后允许再次切人")]
    public float applyNextSwitchTime = 0.45f;
    [Tooltip("旧角色播完切出动画后的退场延迟")]
    public float switchOutCharacterTime = 0.7f;
    [Header("切人镜头平滑过渡")]
    [Tooltip("切人期间镜头锚点跟随新角色的速度（越大越跟手）")]
    public float switchCameraSmoothSpeed = 6f;
    [Tooltip("镜头过渡时长上限（秒）")]
    public float switchCameraTransitionTime = 0.9f;

    [Header("QTE 连携")]
    [Tooltip("时停选人窗口（秒，不计慢动作）")]
    public float qteWindowTime = 3f;
    [Tooltip("QTE 期间的时间缩放（慢动作）")]
    public float qteSlowMotionScale = 0.06f;
    [Tooltip("连携技入场点与敌人的水平距离")]
    public float assistSpawnDistance = 2.4f;
    [Header("QTE 镜头（仅轻微放大当前镜头）")]
    [Tooltip("QTE 期间是否把当前镜头轻微拉近")]
    public bool useQteCamera = true;
    [Tooltip("拉近倍率：小于 1 越近（0.75~0.85 较自然）")]
    public float qteZoomScale = 0.8f;
    [Tooltip("拉近后停留再放慢（秒，真实时间）")]
    public float qteCameraHold = 0.3f;
    [Tooltip("慢动作开始后停留再显示选人 UI（秒，真实时间）")]
    public float qteSlowMotionHold = 0.2f;
    public bool logSwitchEvents = true;

    [Header("成员（可手动在 Inspector 配置，配置后不再自动克隆）")]
    public List<TeamMember> members = new List<TeamMember>();

    public int activeIndex { get; protected set; } = -1;

    /// <summary>当前受控成员；未就绪时返回 null。</summary>
    public Player activePlayer
    {
        get
        {
            if (activeIndex >= 0 && activeIndex < members.Count && members[activeIndex].player != null)
            {
                return members[activeIndex].player;
            }
            return null;
        }
    }

    /// <summary>当前是否处于破防 QTE 选人窗口。</summary>
    public bool IsQteActive => m_qteFlow != null && m_qteFlow.IsActive;
    public bool IsReady => m_ready;

    protected PlayerCamera m_cameraRig;
    protected Enemy m_battleEnemy;
    protected bool m_ready;
    protected bool m_subscribedEnemy;
    protected bool canSwitchInput = true;
    protected Coroutine m_switchCooldownRoutine;
    protected Coroutine m_switchCameraRoutine;
    protected Coroutine m_switchInRoutine;
    protected Transform m_switchCameraAnchor;
    protected readonly Dictionary<Player, Coroutine> m_switchOutCoroutines = new Dictionary<Player, Coroutine>();
    private static bool s_warnedSwitchInMissing;
    protected QteFlow m_qteFlow;
    protected InputAction m_switchAction;

    protected virtual void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }
        instance = this;
        CreateActions();
        EnsureQteFlow();
    }

    protected virtual void OnDestroy()
    {
        m_qteFlow?.CancelQte(false);
        if (m_qteFlow != null)
        {
            m_qteFlow.AssistSelected -= HandleQteAssistSelected;
        }
        if (m_switchCameraRoutine != null)
        {
            StopCoroutine(m_switchCameraRoutine);
            m_switchCameraRoutine = null;
        }
        if (m_switchInRoutine != null)
        {
            StopCoroutine(m_switchInRoutine);
            m_switchInRoutine = null;
        }
        if (m_switchCameraAnchor != null)
        {
            Destroy(m_switchCameraAnchor.gameObject);
            m_switchCameraAnchor = null;
        }
        if (m_switchCooldownRoutine != null)
        {
            StopCoroutine(m_switchCooldownRoutine);
            m_switchCooldownRoutine = null;
        }
        foreach (var pair in m_switchOutCoroutines)
        {
            if (pair.Value != null)
            {
                StopCoroutine(pair.Value);
            }
        }
        m_switchOutCoroutines.Clear();
        UnsubscribeEnemy();
        DisableActions();
        if (instance == this)
        {
            instance = null;
        }
    }

    protected virtual void CreateActions()
    {
        if (m_switchAction == null)
        {
            m_switchAction = new InputAction("TeamSwitch", InputActionType.Button, "<Keyboard>/r");
        }
    }

    protected virtual void DisableActions()
    {
        m_switchAction?.Disable();
    }

    /// <summary>战斗初始化：注册 / 克隆成员并激活 0 号成员。</summary>
    public virtual void SetupBattle(Player basePlayer)
    {
        if (basePlayer == null)
        {
            Debug.LogWarning("[CharacterSwitcher] SetupBattle 缺少基础 Player。");
            return;
        }

        m_cameraRig = Object.FindFirstObjectByType<PlayerCamera>();
        m_battleEnemy = Object.FindFirstObjectByType<Enemy>();
        SubscribeEnemy();

        // 成员来源：手动配置 > 运行时克隆 > 场景已摆放成员
        bool hasManualMembers = members != null && members.Count > 0 && members[0] != null && members[0].player != null;
        if (!hasManualMembers && autoCloneTeam)
        {
            BuildDemoTeam(basePlayer);
        }
        if (!hasManualMembers && useSceneMembers && (members == null || members.Count == 0))
        {
            DiscoverSceneMembers(basePlayer);
        }

        if (members == null || members.Count == 0)
        {
            members = new List<TeamMember> { CreateMember("PLAYER", basePlayer, Color.white) };
        }

        ResolveMissingAnchors();

        // 只保留 0 号成员在场
        for (int i = members.Count - 1; i >= 0; i--)
        {
            if (members[i] == null || members[i].player == null)
            {
                members.RemoveAt(i);
                continue;
            }

            if (i > 0)
            {
                SetMemberVisible(members[i], false);
            }
        }

        if (members.Count == 0)
        {
            Debug.LogWarning("[CharacterSwitcher] 没有任何可用成员。");
            return;
        }

        ActivateMember(0, null, null, false);
        m_ready = true;
        canSwitchInput = true;
        ConfigureQteFlow();
        m_switchAction?.Enable();
        Log("[CharacterSwitcher] 队伍就绪：" + members.Count + " 人，当前 = " + members[0].displayName);
    }

    /// <summary>登记场景中已摆放的角色并按 order 排序。</summary>
    protected virtual void DiscoverSceneMembers(Player basePlayer)
    {
        // 含 inactive：允许候补成员预置为隐藏
        var scenePlayers = Object.FindObjectsByType<Player>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (scenePlayers == null || scenePlayers.Length == 0)
        {
            return;
        }

        var found = new List<TeamMember>();
        for (int i = 0; i < scenePlayers.Length; i++)
        {
            var p = scenePlayers[i];
            if (p == null)
            {
                continue;
            }
            var marker = p.GetComponent<SwitchCharacterMember>()
                ?? p.GetComponentInParent<SwitchCharacterMember>();
            string name = marker != null && !string.IsNullOrEmpty(marker.characterName)
                ? marker.characterName
                : (p == basePlayer ? "PLAYER" : "AGENT " + (found.Count + 1));
            Color accent = marker != null ? marker.accent : GetDemoColor(found.Count);

            found.Add(new TeamMember
            {
                displayName = name,
                player = p,
                followAnchor = FindAnchor(p),
                accent = accent,
            });
        }

        found.Sort((a, b) =>
        {
            int oa = GetMemberOrder(a);
            int ob = GetMemberOrder(b);
            if (oa != ob)
            {
                return oa.CompareTo(ob);
            }
            return string.CompareOrdinal(a.displayName, b.displayName);
        });

        members = found;
        Log("[CharacterSwitcher] 已登记场景成员：" + members.Count + " 人");
    }

    private static int GetMemberOrder(TeamMember member)
    {
        return CharacterSwitchUtils.GetMemberOrder(member);
    }

    protected virtual TeamMember CreateMember(string name, Player player, Color accent)
    {
        return new TeamMember
        {
            displayName = string.IsNullOrEmpty(name) ? "AGENT" : name,
            player = player,
            followAnchor = FindAnchor(player),
            accent = accent,
        };
    }

    /// <summary>克隆场景角色组成队伍：0 号用原角色，其余染色克隆。</summary>
    protected virtual void BuildDemoTeam(Player basePlayer)
    {
        if (members == null)
        {
            members = new List<TeamMember>();
        }
        members.Clear();

        int size = Mathf.Clamp(autoTeamSize, 1, 8);
        GameObject templateRoot = basePlayer.transform.root != null ? basePlayer.transform.root.gameObject : basePlayer.gameObject;
        Vector3 origin = basePlayer.transform.position;
        Quaternion rotation = basePlayer.transform.rotation;
        Vector3 forward = rotation * Vector3.forward;
        Vector3 right = rotation * Vector3.right;

        for (int i = 0; i < size; i++)
        {
            string name = GetDemoName(i);
            Color accent = GetDemoColor(i);

            if (i == 0)
            {
                members.Add(CreateMember(name, basePlayer, accent));
                continue;
            }

            GameObject clone = Instantiate(templateRoot);
            clone.name = "Player_" + name;
            // 初始放在场地边缘等待区
            Vector3 standbyOffset = -forward * 1.6f * i - right * 0.7f;
            clone.transform.position = origin + standbyOffset;
            clone.transform.rotation = rotation;

            Player p = clone.GetComponentInChildren<Player>();
            if (p == null)
            {
                Destroy(clone);
                Debug.LogWarning("[CharacterSwitcher] 克隆体上找不到 Player，已跳过 " + name);
                continue;
            }

            TintRenderer(clone, accent);
            members.Add(CreateMember(name, p, accent));
        }

        Log("[CharacterSwitcher] 已生成队伍：" + members.Count + " 人");
    }

    protected virtual string GetDemoName(int index)
    {
        return CharacterSwitchUtils.GetDemoName(index, demoMemberNames);
    }

    protected virtual Color GetDemoColor(int index)
    {
        return CharacterSwitchUtils.GetDemoColor(index, demoMemberColors);
    }

    protected static void TintRenderer(GameObject root, Color accent)
    {
        CharacterSwitchUtils.TintRenderer(root, accent);
    }

    protected virtual Transform FindAnchor(Player player)
    {
        return CharacterSwitchUtils.FindAnchor(player);
    }

    protected static Transform FindChildByName(Transform root, string name)
    {
        return CharacterSwitchUtils.FindChildByName(root, name);
    }

    protected virtual void ResolveMissingAnchors()
    {
        for (int i = 0; i < members.Count; i++)
        {
            if (members[i] == null || members[i].player == null)
            {
                continue;
            }
            if (members[i].followAnchor == null)
            {
                members[i].followAnchor = FindAnchor(members[i].player);
            }
        }
    }

    protected virtual void SetMemberVisible(TeamMember member, bool visible)
    {
        if (member == null || member.player == null)
        {
            return;
        }

        var inputs = member.player.inputs;
        if (!visible)
        {
            inputs?.actions?.Disable();
            member.player.gameObject.SetActive(false);
            return;
        }

        member.player.gameObject.SetActive(true);
        member.player.enabled = true;
        if (member.player.states != null)
        {
            member.player.states.enabled = true;
        }
        // 首次上场的成员属性可能未就绪，先初始化 current
        if (member.player.stats != null && member.player.stats.current == null
            && member.player.stats.stats != null && member.player.stats.stats.Length > 0)
        {
            member.player.stats.Change(0);
        }
        if (member.player.p_animator != null)
        {
            member.player.p_animator.enabled = true;
        }
        inputs?.actions?.Enable();

        // 清掉离场期间残留的受击请求
        member.player.ConsumeHitRequest();

        // 重新进入时统一回到 Idle
        if (member.player.states != null && Time.timeScale > 0f)
        {
            member.player.states.Change<Player_IdleState>();
        }
    }

    protected virtual void Update()
    {
        if (!m_ready)
        {
            return;
        }

        // QTE 选人期间锁定 R 切人
        if (m_qteFlow != null && m_qteFlow.IsActive)
        {
            return;
        }

        if (m_switchAction != null && m_switchAction.WasPressedThisFrame() && CanSwitchNow())
        {
            SwitchInput();
        }
    }

    protected virtual bool CanSwitchNow()
    {
        if (members == null || members.Count < 2 || Time.timeScale <= 0f)
        {
            return false;
        }
        if (!canSwitchInput)
        {
            return false;
        }
        var p = activePlayer;
        if (p == null || p.states == null)
        {
            return false;
        }
        // 入场演出未结束前不允许再次切人
        if (p.IsSwitchingIn)
        {
            return false;
        }
        // 终结技与连携技演出中不允许打断
        if (p.states.current is Player_SkillState skill
            && (skill.IsFinishSkillAction || skill.currentAction == Player_SkillState.SkillAction.AssistSkill))
        {
            return false;
        }
        return true;
    }

    /// <summary>执行一次切人并进入切换缓冲。</summary>
    public virtual bool SwitchInput()
    {
        if (!CanSwitchNow() || members.Count == 0)
        {
            return false;
        }
        canSwitchInput = false;
        int next = (activeIndex + 1) % members.Count;
        Vector3? spawn = null;
        Quaternion? rot = null;
        var current = activePlayer;
        if (current != null)
        {
            // 从当前角色身后切出
            spawn = current.transform.position - current.transform.forward * switchSpawnDistance;
            rot = current.transform.rotation;
        }
        ActivateMember(next, spawn, rot, true);

        if (m_switchCooldownRoutine != null)
        {
            StopCoroutine(m_switchCooldownRoutine);
        }
        m_switchCooldownRoutine = StartCoroutine(EnableSwitchInputLater());
        return true;
    }

    /// <summary>缓冲时间结束后允许再次切人。</summary>
    protected virtual System.Collections.IEnumerator EnableSwitchInputLater()
    {
        yield return new WaitForSeconds(Mathf.Max(0.05f, applyNextSwitchTime));
        canSwitchInput = true;
        m_switchCooldownRoutine = null;
    }

    protected virtual void ActivateMember(int index, Vector3? spawnPosition, Quaternion? spawnRotation,
        bool playSwitchIn = true)
    {
        if (members == null || index < 0 || index >= members.Count)
        {
            return;
        }

        var next = members[index];
        if (next == null || next.player == null)
        {
            return;
        }

        var old = activePlayer;
        TeamMember oldMember = null;
        if (activeIndex >= 0 && activeIndex < members.Count)
        {
            oldMember = members[activeIndex];
        }
        // 先更新当前成员归属，让索敌与攻击立即认新角色
        activeIndex = index;
        if (old != null && old != next.player && oldMember != null && old.gameObject.activeInHierarchy)
        {
            // 旧角色播完切出动画后延迟退场
            StartSwitchOut(oldMember);
        }
        else if (old != null && old != next.player && old.gameObject.activeInHierarchy)
        {
            old.inputs?.actions?.Disable();
            old.gameObject.SetActive(false);
        }

        if (spawnPosition.HasValue)
        {
            // 只移动玩家本体而非根节点，避免站位逐次漂移
            next.player.transform.position = spawnPosition.Value;
        }
        if (spawnRotation.HasValue)
        {
            // 朝向同样写在本体上
            next.player.transform.rotation = spawnRotation.Value;
        }

        SetMemberVisible(next, true);
        if (playSwitchIn)
        {
            Vector3? landTarget = old != null ? (Vector3?)old.transform.position : null;
            StartSwitchInAnimation(next, landTarget, old);
        }
        if (playSwitchIn && old != null && old != next.player)
        {
            // 镜头先留在旧机位，再平滑过渡到新角色
            StartSwitchCameraTransition(old, next);
        }
        else
        {
            RetargetCamera(next);
        }
        Log("[CharacterSwitcher] 切换到 " + next.displayName);
    }

    /// <summary>切人镜头从旧角色锚点平滑过渡到新角色。</summary>
    protected virtual void StartSwitchCameraTransition(Player oldPlayer, TeamMember next)
    {
        if (m_cameraRig == null)
        {
            m_cameraRig = Object.FindFirstObjectByType<PlayerCamera>();
        }
        if (m_cameraRig == null || oldPlayer == null || next == null || next.player == null)
        {
            RetargetCamera(next);
            return;
        }

        if (m_switchCameraAnchor == null)
        {
            var go = new GameObject("SwitchCameraAnchor");
            go.transform.SetParent(transform, false);
            m_switchCameraAnchor = go.transform;
        }

        Transform fromAnchor = FindAnchor(oldPlayer);
        m_switchCameraAnchor.position = fromAnchor != null ? fromAnchor.position : oldPlayer.transform.position;
        m_cameraRig.Retarget(next.player, m_switchCameraAnchor, m_switchCameraAnchor);

        if (m_switchCameraRoutine != null)
        {
            StopCoroutine(m_switchCameraRoutine);
        }
        m_switchCameraRoutine = StartCoroutine(SmoothSwitchCamera(next));
    }

    protected virtual System.Collections.IEnumerator SmoothSwitchCamera(TeamMember member)
    {
        float elapsed = 0f;
        Transform targetAnchor = member != null && member.followAnchor != null
            ? member.followAnchor
            : (member != null && member.player != null ? FindAnchor(member.player) : null);

        while (m_switchCameraAnchor != null && targetAnchor != null && elapsed < switchCameraTransitionTime)
        {
            elapsed += Time.deltaTime;
            float k = 1f - Mathf.Exp(-switchCameraSmoothSpeed * Time.deltaTime);
            m_switchCameraAnchor.position = Vector3.Lerp(m_switchCameraAnchor.position, targetAnchor.position, k);
            yield return null;
        }

        m_switchCameraRoutine = null;
        if (member != null && member.player != null && member.player == activePlayer)
        {
            RetargetCamera(member);
        }
    }

    /// <summary>旧成员播放切出动画，结束后退场。</summary>
    protected virtual void StartSwitchOut(TeamMember member)
    {
        if (member == null || member.player == null)
        {
            return;
        }
        var p = member.player;
        if (m_switchInRoutine != null)
        {
            StopCoroutine(m_switchInRoutine);
            m_switchInRoutine = null;
        }
        // 若正在入场演出中被切出，先解除入场锁
        p.SetSwitchingIn(false);

        string outName = ResolveSwitchState(p.m_animator, false, switchOutAnimation);
        if (outName == null)
        {
            // 没有切出动画时直接隐藏
            Debug.LogWarning($"[CharacterSwitcher] {p.name} 找不到切出动画（SwitchOut/Switch out/切出），"
                + "请检查 Animator 或 switchOutAnimation 配置。");
            SetMemberVisible(member, false);
            return;
        }

        p.inputs?.SetRunning(false);
        p.SetSuppressRootMotion(false);
        p.inputs?.actions?.Disable();
        // 状态机由 Player 的 Update 驱动，切出时直接禁用 Player
        p.enabled = false;

        // 保持 Animator 启用，让切出根运动正常执行
        if (p.m_animator != null && !p.m_animator.enabled)
        {
            p.m_animator.enabled = true;
        }
        if (p.m_animator != null && p.m_animator.HasState(0, Animator.StringToHash(outName)))
        {
            p.m_animator.CrossFadeInFixedTime(outName, 0.1f);
        }

        if (m_switchOutCoroutines.TryGetValue(p, out var existing) && existing != null)
        {
            StopCoroutine(existing);
            m_switchOutCoroutines.Remove(p);
        }
        var routine = StartCoroutine(FinishSwitchOutLater(p, outName));
        m_switchOutCoroutines[p] = routine;
    }

    protected virtual System.Collections.IEnumerator FinishSwitchOutLater(Player p, string switchOutStateName)
    {
        float maxHold = Mathf.Max(0.1f, switchOutCharacterTime);
        float elapsed = 0f;
        bool sawSwitchOutState = false;

        while (elapsed < maxHold)
        {
            elapsed += Time.deltaTime;
            if (p == null || p.m_animator == null)
            {
                break;
            }

            // 等动画真正进入切出状态再计时，避免误判提前退场
            if (!sawSwitchOutState)
            {
                if (p.m_animator.IsInTransition(0)
                    || IsInAnimatorState(p.m_animator, switchOutStateName))
                {
                    sawSwitchOutState = true;
                }
                else
                {
                    yield return null;
                    continue;
                }
            }

            if (p.m_animator.IsInTransition(0))
            {
                yield return null;
                continue;
            }

            var info = p.m_animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName(switchOutStateName))
            {
                // 切出动画播到末尾提前退场
                if (info.normalizedTime >= 0.98f)
                {
                    break;
                }
            }
            else
            {
                // 已离开切出状态则退场
                break;
            }
            yield return null;
        }

        m_switchOutCoroutines.Remove(p);
        if (p != null && !sawSwitchOutState && p.m_animator != null)
        {
            Debug.LogWarning($"[CharacterSwitcher] {p.name} 的切出动画未观测到真正播放（{switchOutStateName}），旧角色直接退场。");
        }
        if (p != null && p != activePlayer && p.gameObject != null && p.gameObject.activeSelf)
        {
            p.gameObject.SetActive(false);
            // 共享输入资产可能被离场成员的 OnDisable 一并禁用，需要重新开启
            if (m_qteFlow == null || !m_qteFlow.IsActive)
            {
                activePlayer?.inputs?.actions?.Enable();
            }
        }
    }

    /// <summary>
    /// 新成员入场：播放切入动画，开启落地修正时平滑落到旧角色位置。
    /// </summary>
    protected virtual void StartSwitchInAnimation(TeamMember member, Vector3? landPosition, Player oldPlayer = null)
    {
        if (member == null || member.player == null || member.player.m_animator == null)
        {
            return;
        }
        var p = member.player;
        string inName = ResolveSwitchState(p.m_animator, true, switchInAnimation);
        if (inName == null)
        {
            if (!s_warnedSwitchInMissing)
            {
                s_warnedSwitchInMissing = true;
                Debug.LogWarning("[CharacterSwitcher] 找不到切入动画（SwitchIn/Switch in/切入），请检查 Animator 或 switchInAnimation 配置。");
            }
            // 没有入场动画时直接站到旧角色位置
            if (landPosition.HasValue)
            {
                p.transform.position = landPosition.Value;
            }
            return;
        }

        if (m_switchInRoutine != null)
        {
            StopCoroutine(m_switchInRoutine);
            m_switchInRoutine = null;
        }

        // 入场锁：动画播完前状态机忽略输入与受击打断
        p.SetSwitchingIn(true);
        p.m_animator.Play(Animator.StringToHash(inName), 0, 0f);

        if (landSwitchInOnOldPosition && landPosition.HasValue)
        {
            p.SetSuppressRootMotion(true); // 位移交给下方协程
            m_switchInRoutine = StartCoroutine(RunSwitchInLanding(member, landPosition, inName, oldPlayer));
        }
        else
        {
            // 不启用落地修正时也等动画播完再解锁
            m_switchInRoutine = StartCoroutine(RunSwitchInLanding(member, null, inName, oldPlayer));
        }
    }

    /// <summary>入场协程：等待动画结束并落地后解锁。</summary>
    protected virtual System.Collections.IEnumerator RunSwitchInLanding(TeamMember member, Vector3? landPosition,
        string stateName, Player oldPlayer)
    {
        var p = member != null ? member.player : null;
        Transform body = p != null ? p.transform : null;
        float elapsed = 0f;
        float maxHold = Mathf.Max(0.05f, switchInHoldTime);
        bool landing = landPosition.HasValue;

        while (body != null && p != null && p == activePlayer && elapsed < maxHold)
        {
            elapsed += Time.deltaTime;

            if (landing)
            {
                bool oldStillOnTarget = oldPlayer != null && oldPlayer != p && oldPlayer.gameObject != null
                    && oldPlayer.gameObject.activeSelf
                    && Vector3.Distance(oldPlayer.transform.position, landPosition.Value) < 0.1f;
                if (!oldStillOnTarget)
                {
                    body.position = Vector3.MoveTowards(body.position, landPosition.Value,
                        switchInLandSpeed * Time.deltaTime);
                }
            }

            bool animDone = p.m_animator != null
                && !p.m_animator.IsInTransition(0)
                && !IsInAnimatorState(p.m_animator, stateName);
            bool atTarget = !landing || Vector3.Distance(body.position, landPosition.Value) <= 0.02f;
            if (animDone && atTarget)
            {
                break;
            }
            yield return null;
        }

        m_switchInRoutine = null;
        if (p != null)
        {
            p.SetSuppressRootMotion(false);
            p.SetSwitchingIn(false);
            if (p == activePlayer && body != null && landing)
            {
                body.position = landPosition.Value;
            }
        }
    }

    private static bool IsInAnimatorState(Animator animator, string stateName)
    {
        return CharacterSwitchUtils.IsInAnimatorState(animator, stateName);
    }

    /// <summary>解析切入 / 切出动画状态名。</summary>
    private static string ResolveSwitchState(Animator animator, bool isIn, string configured)
    {
        return CharacterSwitchUtils.ResolveSwitchState(animator, isIn, configured);
    }

    protected virtual void RetargetCamera(TeamMember member)
    {
        if (m_cameraRig == null)
        {
            m_cameraRig = Object.FindFirstObjectByType<PlayerCamera>();
        }
        if (m_cameraRig != null && member != null)
        {
            Transform follow = member.followAnchor != null ? member.followAnchor : member.player.transform;
            m_cameraRig.Retarget(member.player, follow, follow);
        }
    }

    // ===== QTE 连携 =====

    protected virtual void SubscribeEnemy()
    {
        if (m_subscribedEnemy || m_battleEnemy == null)
        {
            return;
        }
        m_battleEnemy.OnStunEnter += HandleEnemyStunEnter;
        m_battleEnemy.OnStunExit += HandleEnemyStunExit;
        m_subscribedEnemy = true;
    }

    protected virtual void UnsubscribeEnemy()
    {
        if (m_subscribedEnemy && m_battleEnemy != null)
        {
            m_battleEnemy.OnStunEnter -= HandleEnemyStunEnter;
            m_battleEnemy.OnStunExit -= HandleEnemyStunExit;
        }
        m_subscribedEnemy = false;
    }

    protected virtual void HandleEnemyStunEnter(Enemy enemy)
    {
        if (enemy == null || enemy.isDead || IsQteActive || !m_ready)
        {
            return;
        }
        if (members.Count < 2 || activePlayer == null)
        {
            return;
        }

        m_qteFlow?.StartQte(enemy);
    }

    protected virtual void HandleEnemyStunExit(Enemy enemy)
    {
        if (m_qteFlow != null && m_qteFlow.IsActive && enemy != null && enemy == m_qteFlow.currentEnemy)
        {
            m_qteFlow.CancelQte(false);
        }
    }

    /// <summary>懒创建 QTE 控制器。</summary>
    protected virtual void EnsureQteFlow()
    {
        if (m_qteFlow != null)
        {
            return;
        }
        m_qteFlow = GetComponent<QteFlow>();
        if (m_qteFlow == null)
        {
            m_qteFlow = gameObject.AddComponent<QteFlow>();
        }
    }

    /// <summary>把 QTE 配置注入 QteFlow 并订阅连携事件。</summary>
    protected virtual void ConfigureQteFlow()
    {
        EnsureQteFlow();
        if (m_qteFlow == null)
        {
            return;
        }

        m_qteFlow.Configure(
            useQteCamera,
            qteZoomScale,
            qteCameraHold,
            qteSlowMotionScale,
            qteSlowMotionHold,
            qteWindowTime,
            logSwitchEvents,
            () => members != null ? members.Count : 0,
            () => activeIndex,
            GetMemberDisplayName,
            GetMemberAccent);
        m_qteFlow.AssistSelected -= HandleQteAssistSelected;
        m_qteFlow.AssistSelected += HandleQteAssistSelected;
    }

    protected virtual string GetMemberDisplayName(int index)
    {
        if (index >= 0 && index < members.Count && members[index] != null)
        {
            return members[index].displayName;
        }
        return "-";
    }

    protected virtual Color GetMemberAccent(int index)
    {
        if (index >= 0 && index < members.Count && members[index] != null)
        {
            return members[index].accent;
        }
        return Color.white;
    }

    /// <summary>QTE 选人完成：切到目标成员并在敌人身边入场。</summary>
    protected virtual void HandleQteAssistSelected(int index, Enemy enemy)
    {
        if (members == null || index < 0 || index >= members.Count)
        {
            return;
        }
        if (enemy == null || enemy.isDead)
        {
            return;
        }

        var old = activePlayer;
        Vector3 spawn;
        Quaternion rot;
        if (old != null)
        {
            Vector3 dir = enemy.transform.position - old.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
            {
                dir = old.transform.forward;
            }
            dir.Normalize();
            spawn = enemy.transform.position - dir * assistSpawnDistance;
            rot = Quaternion.LookRotation(dir, Vector3.up);
        }
        else
        {
            spawn = enemy.transform.position - enemy.transform.forward * assistSpawnDistance;
            rot = Quaternion.LookRotation(-enemy.transform.forward, Vector3.up);
        }

        ActivateMember(index, spawn, rot, false);

        // 克隆成员的状态机延迟到激活帧启动，先等待再触发连携
        StartCoroutine(ExecuteAssistAfterStateStartup(index));

        CameraShakeManager.instance?.Shake(0.5f, 0.22f);
        Log("[CharacterSwitcher] 连携技：" + GetMemberDisplayName(index));
    }

    protected virtual System.Collections.IEnumerator ExecuteAssistAfterStateStartup(int index)
    {
        int guard = 0;
        var p = index >= 0 && index < members.Count && members[index] != null ? members[index].player : null;
        while (p != null && p.states != null && p.states.current == null && guard < 8)
        {
            yield return null;
            guard++;
        }

        if (p == null || p.states == null || index != activeIndex)
        {
            yield break;
        }
        if (Player_SkillState.HasConfiguredAction(p, Player_SkillState.SkillAction.AssistSkill))
        {
            p.RequestSkill(Player_SkillState.SkillAction.AssistSkill);
            p.states.Change<Player_SkillState>();
        }
    }

    protected virtual void Log(string message)
    {
        if (logSwitchEvents)
        {
            GameLog.Log(message);
        }
    }
}
