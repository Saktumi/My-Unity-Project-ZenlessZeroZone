using Unity.Cinemachine;
using UnityEngine;

/// <summary>玩家实体：整合输入、属性、动画与状态机，对外提供战斗接口。</summary>
public class Player : Entity<Player>
{
    public PlayerInputManager inputs { get; protected set; }
    public PlayerStatsManager stats { get; protected set; }

    public Animator m_animator { get; protected set; }
    public PlayerAnimator p_animator { get; protected set; }

    public CinemachineImpulseSource impulseSource { get; protected set; }

    protected override void Awake()
    {
        base.Awake();
        InitializeInputs();
        InitializeStats();
        InitializeMAnimator();
        InitializePAnimator();
        InitializeImpulseSource();

        // 终结技镜头 / 演出、吸附与命中逻辑拆到独立类，Player 只负责转发
        m_finishSkillCameraDriver = new FinishSkillCameraDriver(this);
        m_finishSkillCameraDriver.DisableCamerasAtStart();
        m_finishSkillDirector = new FinishSkillDirector(this);
        m_attackSnap = new PlayerAttackSnap(this);
        m_hitDetector = new PlayerHitDetector(this);
    }

    protected virtual void InitializeInputs() => inputs = GetComponent<PlayerInputManager>();

    protected virtual void InitializeStats() => stats = GetComponent<PlayerStatsManager>();

    protected virtual void InitializeMAnimator() => m_animator = GetComponent<Animator>();

    public virtual void InitializePAnimator() => p_animator = GetComponent<PlayerAnimator>();

    protected virtual void InitializeImpulseSource() => impulseSource = GetComponent<CinemachineImpulseSource>();

    protected virtual void OnDestroy()
    {
        // 场景卸载或玩家销毁时恢复被隐藏的场景与演出天空
        m_finishSkillDirector?.End();
    }

    protected virtual void OnEnable()
    {
        PlayerRegistry.Register(this);
    }

    protected virtual void OnDisable()
    {
        PlayerRegistry.Unregister(this);
    }

    // ===== Evade 连续闪避冷却 =====
    public float evadeCooldownRemaining { get; protected set; }
    public float evadeChainWindowEndTime { get; protected set; }

    public bool CanEvade => evadeCooldownRemaining <= 0f;

    // 第一段闪避播完后进入衔接窗口
    public bool CanChainEvade => Time.time <= evadeChainWindowEndTime;

    public virtual void TickEvadeCooldown()
    {
        if (evadeCooldownRemaining > 0f)
        {
            evadeCooldownRemaining = Mathf.Max(0f, evadeCooldownRemaining - Time.deltaTime);
        }
    }

    public virtual void StartEvadeCooldown()
    {
        float cooldown = stats != null && stats.current != null ? stats.current.evadeCooldown : 2f;
        evadeCooldownRemaining = Mathf.Max(evadeCooldownRemaining, cooldown);
    }

    public virtual void OpenEvadeChainWindow()
    {
        float window = stats != null && stats.current != null ? stats.current.evadeChainWindow : 1f;
        evadeChainWindowEndTime = Time.time + window;
    }

    // ===== 闪避后冲刺攻击窗口 =====
    public float rushAttackWindowEndTime { get; protected set; }

    public bool CanRushAttack => Time.time <= rushAttackWindowEndTime;

    public virtual void OpenRushAttackWindow() => rushAttackWindowEndTime = Time.time + 0.5f;

    public virtual void ConsumeRushAttack() => rushAttackWindowEndTime = 0f;

    // ===== 根运动位移控制 =====
    public bool suppressRootMotion { get; protected set; }

    /// <summary>临时禁用 / 恢复根运动位移。</summary>
    public virtual void SetSuppressRootMotion(bool value)
    {
        suppressRootMotion = value;
    }

    // ===== 切换入场演出锁 =====
    /// <summary>是否处于切入演出中，置位期间状态机忽略输入与受击打断。</summary>
    public bool IsSwitchingIn { get; protected set; }

    public virtual void SetSwitchingIn(bool value)
    {
        IsSwitchingIn = value;
    }

    // ===== 技能输入请求（进入 Player_SkillState 前标记要播哪个技能） =====
    public Player_SkillState.SkillAction requestedSkillAction { get; protected set; }

    public virtual void RequestSkill(Player_SkillState.SkillAction action)
    {
        requestedSkillAction = action;
    }

    public virtual Player_SkillState.SkillAction ConsumeRequestedSkill()
    {
        var action = requestedSkillAction;
        requestedSkillAction = Player_SkillState.SkillAction.None;
        return action;
    }

    // ===== 强化特殊技能量 =====
    private float m_specialSkillEnergy = -1f;

    public float specialSkillEnergy
    {
        get
        {
            // stats.current 在 EntityStatsManager.Start 才就绪，首次访问时初始化为满
            if (m_specialSkillEnergy < 0f)
            {
                var current = stats != null ? stats.current : null;
                m_specialSkillEnergy = current != null ? current.maxSpecialSkillEnergy : 0f;
            }
            return m_specialSkillEnergy;
        }
        protected set
        {
            m_specialSkillEnergy = value;
        }
    }

    public bool CanUseSpecialSkill
    {
        get
        {
            var current = stats != null ? stats.current : null;
            float cost = current != null ? current.specialSkillEnergyCost : 0f;
            return specialSkillEnergy >= cost;
        }
    }

    public virtual void SetSpecialSkillEnergy(float value)
    {
        specialSkillEnergy = Mathf.Max(0f, value);
    }

    public virtual void ConsumeSpecialSkillEnergy()
    {
        var current = stats != null ? stats.current : null;
        float cost = current != null ? current.specialSkillEnergyCost : 0f;
        specialSkillEnergy = Mathf.Max(0f, specialSkillEnergy - cost);
    }

    // ===== 生命值 / 受击 =====
    private float m_currentHp = -1f;
    private PlayerHitRequest m_pendingHit;

    /// <summary>最大生命值（来自当前 PlayerStats.Hp）。</summary>
    public float maxHp
    {
        get
        {
            var current = stats != null ? stats.current : null;
            return current != null ? Mathf.Max(0f, current.Hp) : 0f;
        }
    }

    /// <summary>当前生命值：stats.current 就绪前首次访问时按最大生命值初始化。</summary>
    public float currentHp
    {
        get
        {
            var current = stats != null ? stats.current : null;
            if (m_currentHp < 0f)
            {
                // stats 未就绪时不能缓存 0，否则血条会永久显示为 0
                if (current == null)
                {
                    return 0f;
                }
                m_currentHp = Mathf.Max(0f, current.Hp);
            }
            return m_currentHp;
        }
        protected set
        {
            m_currentHp = Mathf.Clamp(value, 0f, maxHp);
        }
    }

    public bool isDead => currentHp <= 0f;

    /// <summary>闪避期间无敌。</summary>
    public bool IsInvincible => states != null && states.current is Player_EvadeState;

    /// <summary>是否处于受击硬直中。</summary>
    public bool IsInHitReaction => states != null && states.current is Player_HitState;

    /// <summary>头顶位置（世界坐标），供伤害飘字等表现定位。</summary>
    public virtual Vector3 GetHeadPosition(float extraOffset = 0f)
    {
        float headHeight = controller != null
            ? controller.height * Mathf.Max(0.01f, transform.lossyScale.y) * 0.9f
            : 1.2f;
        return transform.position + Vector3.up * (headHeight + extraOffset);
    }

    /// <summary>不在闪避无敌且未死亡时即可被命中。</summary>
    public bool CanBeHit => !IsInvincible && !isDead;

    public PlayerHitRequest pendingHit => m_pendingHit;

    public virtual bool HasPendingHit => m_pendingHit != null;

    /// <summary>取出并清除受击请求（Player_HitState.OnEnter 调用）。</summary>
    public virtual PlayerHitRequest ConsumeHitRequest()
    {
        var request = m_pendingHit;
        m_pendingHit = null;
        return request;
    }

    /// <summary>受到伤害：扣除生命并生成受击请求，返回是否真正命中。</summary>
    public virtual bool TakeDamage(float damage, Vector3 attackerPosition)
    {
        var current = stats != null ? stats.current : null;
        if (current == null || damage <= 0f || !CanBeHit)
        {
            return false;
        }

        currentHp = currentHp - damage;

        // 硬直中只结算伤害，不重复打断当前受击动画
        if (!IsInHitReaction)
        {
            m_pendingHit = new PlayerHitRequest(damage, attackerPosition);
        }

        GameLog.Log($"[Player] 受到 {damage:0.##} 伤害，HP {currentHp:0.##}/{maxHp:0.##}");

        if (currentHp <= 0f)
        {
            Debug.LogWarning("[Player] 生命值归零。");
        }

        return true;
    }

    // ===== 终结技镜头（序列化字段保留在 Player 上，逻辑见 FinishSkillCameraDriver） =====
    [Header("Finish Skill Camera")]
    public Camera finishSkillCamera1;
    public Camera finishSkillCamera2;
    [Tooltip("事件2触发时相机2沿自身Z轴（前方向）推进的距离，负数为后退")]
    public float finishSkillCamera2ZDolly = 1f;
    [Tooltip("推进动画时长（秒）")]
    public float finishSkillCamera2DollyTime = 0.3f;

    protected FinishSkillCameraDriver m_finishSkillCameraDriver;

    // ===== 攻击吸附 =====
    protected PlayerAttackSnap m_attackSnap;

    /// <summary>攻击起手向最近敌人转向并小幅拉近，返回是否吸附成功。</summary>
    public virtual bool TryAttackSnap() => m_attackSnap != null && m_attackSnap.TrySnap();

    // ===== 命中检测 / 震屏 =====
    protected PlayerHitDetector m_hitDetector;

    /// <summary>攻击动画命中窗口内结算伤害，causesStun 决定敌人进入眩晕或普通受击。</summary>
    public virtual void TickHitDetection(float normalizedTime, float damage, bool causesStun)
        => m_hitDetector?.Tick(normalizedTime, damage, causesStun);

    /// <summary>每段攻击起手时重置命中标记。</summary>
    public virtual void ResetHitDetection() => m_hitDetector?.ResetTrigger();

    /// <summary>普攻 / 冲刺攻击基础伤害（PlayerStats.Attack）。</summary>
    public virtual float GetNormalAttackDamage()
    {
        var current = stats != null ? stats.current : null;
        return current != null ? current.Attack : 0f;
    }

    /// <summary>按技能动作计算基础伤害（特殊技 / 强化 / 终结技 / 连携）。</summary>
    public virtual float GetSkillAttackDamage(Player_SkillState.SkillAction action)
    {
        var current = stats != null ? stats.current : null;
        if (current == null)
        {
            return 0f;
        }

        switch (action)
        {
            case Player_SkillState.SkillAction.Skill:
                return current.SkillAttack;
            case Player_SkillState.SkillAction.SpecialSkill:
                return current.SkillAttack * current.SpecialSkillMultiple;
            case Player_SkillState.SkillAction.AssistSkill:
                return current.SkillAttack * current.assistSkillMultiple;
            case Player_SkillState.SkillAction.FinishSkill:
                return current.SkillAttack * current.FinishSkillMultiple;
            default:
                return 0f; // FinishSkillPass 收尾动画不结算伤害
        }
    }

    /// <summary>命中是否使敌人进入眩晕。</summary>
    public virtual bool IsStunCausingSkill(Player_SkillState.SkillAction action)
    {
        return action == Player_SkillState.SkillAction.SpecialSkill
            || action == Player_SkillState.SkillAction.FinishSkill;
    }

    /// <summary>按暴击率与暴击倍率结算伤害。</summary>
    public virtual float RollDamage(float baseDamage)
    {
        return RollDamage(baseDamage, out _);
    }

    /// <summary>按暴击率与暴击倍率结算伤害，并输出是否暴击。</summary>
    public virtual float RollDamage(float baseDamage, out bool isCrit)
    {
        var current = stats != null ? stats.current : null;
        if (current == null || baseDamage <= 0f)
        {
            isCrit = false;
            return baseDamage;
        }

        isCrit = Random.value < Mathf.Clamp01(current.criticalPer);
        return isCrit ? baseDamage * current.criticalMultiple : baseDamage;
    }

    /// <summary>命中震屏：优先走 CameraShakeManager，否则回退到 Cinemachine Impulse。</summary>
    public virtual void PlayHitImpulse(float force)
    {
        var shaker = CameraShakeManager.Ensure();
        if (shaker != null)
        {
            shaker.Shake(force);
            return;
        }
        impulseSource?.GenerateImpulseWithForce(force);
    }

    /// <summary>动画事件回调：1 / 2 切换特写相机，其它值恢复原相机并还原场景。</summary>
    public virtual void OnFinishSkillCameraEvent(int index)
    {
        // 切回原相机时一并还原被隔离的场景与演出天空
        if (index != 1 && index != 2)
        {
            RestoreFinishSkillSceneAndSky();
        }

        m_finishSkillCameraDriver?.OnEvent(index);
    }

    /// <summary>进入 finishskill 时记录当前相机（转发给镜头驱动器）。</summary>
    public virtual void PrepareFinishSkillCamera()
    {
        m_finishSkillCameraDriver?.Prepare();
    }

    /// <summary>恢复原相机（转发给镜头驱动器）。</summary>
    public virtual void RestoreFinishSkillCamera()
    {
        m_finishSkillCameraDriver?.Restore();
    }

    // ===== 终结技演出（场景隔离 + Height Fog Global 深蓝天空 + 命中结算） =====
    protected FinishSkillDirector m_finishSkillDirector;

    /// <summary>大招起手：隔离场景、切换演出天空并闪现到目标身边。</summary>
    public virtual bool BeginFinishSkillCinematic() => m_finishSkillDirector != null && m_finishSkillDirector.Begin();

    /// <summary>大招动画每帧推进。</summary>
    public virtual void TickFinishSkillCinematic(float normalizedTime) => m_finishSkillDirector?.Tick(normalizedTime);

    /// <summary>大招结束：统一恢复被隐藏的场景与演出天空。</summary>
    public virtual void EndFinishSkillCinematic() => m_finishSkillDirector?.End();

    /// <summary>大招第三次切屏时还原场景与演出天空。</summary>
    public virtual void RestoreFinishSkillSceneAndSky() => m_finishSkillDirector?.RestoreSceneAndSky();

    /// <summary>以属性中配置的旋转速度平滑转向。</summary>
    public virtual void FaceDirectionSmooth(Vector3 direction)
    {
        var current = stats != null ? stats.current : null;
        float rotationSpeed = current != null ? current.rotationSpeed : 970f;
        FaceDirection(direction, rotationSpeed);
    }

}
