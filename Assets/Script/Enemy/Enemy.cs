using UnityEngine;

/// <summary>
/// 敌人实体：整合属性、动画、状态机、索敌与战斗接口，
/// 探测与战斗请求分别由独立类负责。
/// </summary>
public class Enemy : Entity<Enemy>
{
    public EnemyStatsManager stats { get; protected set; }

    public Animator m_animator { get; protected set; }
    public EnemyAnimator e_animator { get; protected set; }

    [Header("Combat Target")]
    [Tooltip("索敌目标（可由外部 AI 脚本赋值）")]
    public Transform target;

    // 索敌与战斗请求由独立类负责
    protected EnemyTargetDetector m_detector;
    protected EnemyCombat m_combat;
    protected Renderer m_cachedRenderer;

    protected override void Awake()
    {
        base.Awake();
        InitializeStats();
        InitializeMAnimator();
        InitializeEAnimator();
        m_detector = new EnemyTargetDetector(this);
        m_combat = new EnemyCombat(this);
        m_cachedRenderer = GetComponentInChildren<Renderer>();
        // 战斗内所有敌人统一登记，供索敌 / 命中查询复用
        EnemyRegistry.Register(this);
    }

    protected virtual void OnDestroy()
    {
        EnemyRegistry.Unregister(this);
    }

    protected override void Update()
    {
        // 死亡后停止索敌与失衡等更新
        if (!deathLocked)
        {
            TickDetection();
            TickDazeRegen();
            TickStunEvents();
        }
        base.Update();
    }

    /// <summary>每帧检测索敌。</summary>
    protected virtual void TickDetection()
    {
        m_detector?.Tick();
    }

    protected virtual void InitializeStats() => stats = GetComponent<EnemyStatsManager>();

    protected virtual void InitializeMAnimator() => m_animator = GetComponent<Animator>();

    public virtual void InitializeEAnimator() => e_animator = GetComponent<EnemyAnimator>();

    // ===== 索敌 / 距离 =====
    public bool HasTarget => target != null;

    public float DistanceToTarget
    {
        get => HorizontalDistance(target);
    }

    public float CurrentAttackRange
    {
        get
        {
            var current = stats != null ? stats.current : null;
            return current != null ? current.attackRange : 2f;
        }
    }

    // ===== 移动方向（AI 脚本可覆写） =====
    // 目标在攻击范围外时追击，进入范围后停下
    public virtual Vector3 GetMovementDirection()
    {
        if (!HasTarget || DistanceToTarget <= CurrentAttackRange)
        {
            return Vector3.zero;
        }

        var toTarget = target.position - transform.position;
        toTarget.y = 0f;
        return toTarget.normalized;
    }

    public virtual void FaceDirectionSmooth(Vector3 direction)
    {
        var current = stats != null ? stats.current : null;
        float rotationSpeed = current != null ? current.rotationSpeed : 720f;
        FaceDirection(direction, rotationSpeed);
    }

    public virtual void FaceTargetSmooth()
    {
        if (!HasTarget)
        {
            return;
        }

        var toTarget = target.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude > 0.001f)
        {
            FaceDirectionSmooth(toTarget.normalized);
        }
    }

    // ===== 攻击请求与冷却（逻辑委托给 EnemyCombat） =====
    public float attackCooldownRemaining => m_combat != null ? m_combat.attackCooldownRemaining : 0f;

    public bool CanAttack => attackCooldownRemaining <= 0f;

    public virtual bool IsAttackRequested() => m_combat != null && m_combat.IsAttackRequested();

    /// <summary>目标在攻击范围内且冷却结束时自动攻击。</summary>
    public virtual bool CanAutoAttack => HasTarget && DistanceToTarget <= CurrentAttackRange && CanAttack;

    /// <summary>显式请求一次攻击。</summary>
    public virtual void RequestAttack() => m_combat?.RequestAttack();

    public virtual bool ConsumeAttackRequest() => m_combat != null && m_combat.ConsumeAttackRequest();

    public virtual void TickAttackCooldown() => m_combat?.TickAttackCooldown();

    public virtual void StartAttackCooldown() => m_combat?.StartAttackCooldown();

    private float HorizontalDistance(Transform other)
    {
        if (other == null)
        {
            return float.PositiveInfinity;
        }

        var offset = other.position - transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }

    // ===== 受击 / 眩晕 / 死亡请求（由外部伤害系统调用，逻辑委托给 EnemyCombat） =====

    public virtual void RequestHit() => m_combat?.RequestHit();

    /// <summary>请求破防眩晕：拉满失衡值后进入眩晕状态。</summary>
    public virtual void RequestStun()
    {
        if (isDead)
        {
            return;
        }
        currentDaze = maxDaze;
        m_combat?.RequestStun();
    }

    public virtual void RequestDeath()
    {
        // 死亡请求发出后立即锁定，禁止其它状态切换
        deathLocked = true;
        m_combat?.RequestDeath();
    }

    /// <summary>进入死亡时清理遗留请求。</summary>
    public virtual void ClearCombatRequests() => m_combat?.ClearRequests();

    // ===== 生命值 / 伤害 =====
    private float m_currentHp = -1f;

    /// <summary>最大生命值。</summary>
    public float maxHp
    {
        get
        {
            var current = stats != null ? stats.current : null;
            return current != null ? Mathf.Max(0f, current.Hp) : 0f;
        }
    }

    /// <summary>当前生命值。</summary>
    public float currentHp
    {
        get
        {
            if (m_currentHp < 0f)
            {
                m_currentHp = maxHp;
            }
            return m_currentHp;
        }
        protected set
        {
            m_currentHp = Mathf.Clamp(value, 0f, maxHp);
        }
    }

    public bool isDead => currentHp <= 0f;

    /// <summary>死亡锁：置位后禁止切换到非死亡状态。</summary>
    public bool deathLocked { get; protected set; }

    // ===== 失衡 / 破防（Daze / Break） =====
    /// <summary>进入 / 退出破防眩晕的事件，用于开关 QTE 窗口。</summary>
    public event System.Action<Enemy> OnStunEnter;
    public event System.Action<Enemy> OnStunExit;

    protected bool m_wasStunned;
    protected float m_currentDaze = -1f;
    protected float m_lastDazeHitTime = -999f;

    /// <summary>失衡值上限。</summary>
    public float maxDaze
    {
        get
        {
            var current = stats != null ? stats.current : null;
            return current != null ? Mathf.Max(0f, current.maxDaze) : 0f;
        }
    }

    /// <summary>当前失衡值：0 ~ maxDaze，打满即破防。</summary>
    public float currentDaze
    {
        get
        {
            if (m_currentDaze < 0f)
            {
                m_currentDaze = 0f;
            }
            return m_currentDaze;
        }
        protected set
        {
            m_currentDaze = Mathf.Clamp(value, 0f, maxDaze);
        }
    }

    /// <summary>是否正处于破防眩晕（StunState）中。</summary>
    public bool IsDazeBroken => states != null && states.current is Enemy_StunState;

    /// <summary>头顶位置（世界坐标），供世界血条 / 伤害飘字定位使用。</summary>
    public virtual Vector3 GetHeadPosition(float extraOffset = 0f)
    {
        float topY = transform.position.y;
        if (m_cachedRenderer == null)
        {
            m_cachedRenderer = GetComponentInChildren<Renderer>();
        }
        Renderer renderer = m_cachedRenderer;
        if (renderer != null && renderer.bounds.size.sqrMagnitude > 0.0001f)
        {
            topY = renderer.bounds.max.y;
        }
        else if (controller != null)
        {
            topY = transform.position.y + controller.height * Mathf.Max(0.01f, transform.lossyScale.y);
        }
        return new Vector3(transform.position.x, topY + extraOffset, transform.position.z);
    }

    /// <summary>累积失衡值并记录最近受击时间。</summary>
    public virtual void GainDaze(float amount)
    {
        if (amount <= 0f || maxDaze <= 0f)
        {
            return;
        }
        currentDaze = currentDaze + amount;
        m_lastDazeHitTime = Time.time;
    }

    /// <summary>失衡值自然回复。</summary>
    protected virtual void TickDazeRegen()
    {
        if (isDead || IsDazeBroken || maxDaze <= 0f)
        {
            return;
        }
        var current = stats != null ? stats.current : null;
        if (current == null)
        {
            return;
        }

        if (Time.time - m_lastDazeHitTime >= current.dazeDecayDelay && currentDaze > 0f)
        {
            currentDaze = currentDaze - current.dazeDecayPerSecond * Time.deltaTime;
        }
    }

    /// <summary>轮询破防状态变化并向外部发事件。</summary>
    protected virtual void TickStunEvents()
    {
        bool stunned = IsDazeBroken;
        if (stunned && !m_wasStunned)
        {
            m_wasStunned = true;
            OnStunEnter?.Invoke(this);
        }
        else if (!stunned && m_wasStunned)
        {
            m_wasStunned = false;
            currentDaze = 0f;
            OnStunExit?.Invoke(this);
        }
    }

    /// <summary>
    /// 受到伤害：扣除生命并生成受击 / 破防 / 死亡请求。
    /// </summary>
    public virtual void TakeDamage(float damage, bool stunHit)
    {
        if (isDead || damage <= 0f)
        {
            return;
        }

        bool alreadyBroken = IsDazeBroken;
        if (!alreadyBroken)
        {
            if (stunHit)
            {
                currentDaze = maxDaze;
            }
            else
            {
                float ratio = stats != null && stats.current != null ? stats.current.dazeGainRatio : 1f;
                GainDaze(damage * ratio);
            }
        }

        currentHp = currentHp - damage;

        GameLog.Log($"[Enemy] 受到 {damage:0.##} 伤害（stunHit={stunHit}），HP {currentHp:0.##}/{maxHp:0.##}");

        if (currentHp <= 0f)
        {
            RequestDeath();
        }
        else if (alreadyBroken)
        {
            // 破防期间只结算伤害
            return;
        }
        else if (maxDaze > 0f && currentDaze >= maxDaze)
        {
            RequestStun();
        }
        else
        {
            RequestHit();
        }
    }

    /// <summary>终结技演出专用伤害结算。</summary>
    public virtual void TakeUltimateDamage(float damage)
    {
        if (isDead || damage <= 0f)
        {
            return;
        }

        bool alreadyBroken = IsDazeBroken;
        if (!alreadyBroken)
        {
            float ratio = stats != null && stats.current != null ? stats.current.dazeGainRatio : 1f;
            GainDaze(damage * ratio);
        }

        currentHp = currentHp - damage;

        GameLog.Log($"[Enemy] 终结技命中 {damage:0.##} 伤害，HP {currentHp:0.##}/{maxHp:0.##}");

        if (currentHp <= 0f)
        {
            RequestDeath();
        }
        else if (alreadyBroken)
        {
            // 破防期间只结算伤害
            return;
        }
        else if (maxDaze > 0f && currentDaze >= maxDaze)
        {
            RequestStun();
        }
    }

    public virtual bool ConsumeHitRequest() => m_combat != null && m_combat.ConsumeHitRequest();

    public virtual bool ConsumeStunRequest() => m_combat != null && m_combat.ConsumeStunRequest();

    public virtual bool ConsumeDeathRequest() => m_combat != null && m_combat.ConsumeDeathRequest();
}
