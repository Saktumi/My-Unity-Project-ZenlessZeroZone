using System.Collections.Generic;
using AtmosphericHeightFog;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 终结技演出导演：大招期间隔离场景、切换深蓝雾效天空，
/// 闪现到最近敌人身边并在命中时机结算一次伤害，结束后统一还原。
/// </summary>
public class FinishSkillDirector
{
    private const float k_DefaultSnapDistance = 1.6f;
    private const float k_DefaultHitTime = 0.32f;

    // 演出天空使用的深蓝雾色
    private static readonly Color k_DeepBlueSkyStart = new Color(0.02f, 0.05f, 0.28f);
    private static readonly Color k_DeepBlueSkyEnd = new Color(0.06f, 0.11f, 0.45f);

    private readonly Player m_player;

    private bool m_running;
    private Enemy m_target;
    private bool m_hitDealt;
    private GameObject m_playerRoot;

    // ===== 场景隔离：只留角色 =====
    private readonly List<RootState> m_hiddenRoots = new List<RootState>();

    private struct RootState
    {
        public GameObject root;
        public bool wasActive;
    }

    // ===== 大招期间隐藏的敌人 HUD =====
    private readonly List<GameObject> m_hiddenEnemyUi = new List<GameObject>();
    private readonly List<bool> m_hiddenEnemyUiWasActive = new List<bool>();

    // ===== Height Fog Global 演出天空 =====
    private HeightFogGlobal m_fog;
    private GameObject m_fogObject;
    private bool m_fogCreatedByUs;
    private bool m_fogWasActive;
    private bool m_fogValuesBackedUp;
    private AtmosphericHeightFog.FogMode m_fogModeBackup;
    private Color m_fogColorStartBackup;
    private Color m_fogColorEndBackup;
    private float m_fogIntensityBackup;
    private float m_skyboxFogFillBackup;

    public FinishSkillDirector(Player player)
    {
        m_player = player;
    }

    public bool isRunning => m_running;

    /// <summary>大招起手：隐藏场景中的其它物体、切到深蓝雾效天空、闪现到最近敌人身边。</summary>
    public bool Begin()
    {
        if (m_running)
        {
            return m_target != null;
        }

        m_running = true;
        m_hitDealt = false;
        m_playerRoot = GetSceneRoot(m_player.transform);
        m_target = FindNearestEnemy();

        // 先准备雾效天空，隔离场景时需要保留它的根物体
        if (WantIsolateScene())
        {
            PrepareFogSky();
            IsolateScene();
        }
        HideEnemyBars();

        if (m_target == null)
        {
            GameLog.Log("[FinishSkillDirector] 没有可攻击的敌人，大招只播放动画演出。");
            return false;
        }

        SnapToTarget(m_target);
        GameLog.Log($"[FinishSkillDirector] 大招起手：场景只保留角色，天空切到 Height Fog Global，锁定 {m_target.name}。");
        return true;
    }

    /// <summary>finishskill 动画每帧推进：到达命中时机结算一次伤害。</summary>
    public void Tick(float normalizedTime)
    {
        if (!m_running || m_target == null)
        {
            return;
        }

        TryDealHit(normalizedTime);
    }

    /// <summary>大招结束：统一恢复场景、演出天空与敌人 HUD。</summary>
    public void End()
    {
        if (!m_running)
        {
            return;
        }

        m_running = false;
        RestoreScene();
        RestoreFogSky();
        RestoreEnemyBars();

        m_target = null;
        m_hitDealt = false;
        m_playerRoot = null;
    }

    /// <summary>第三次切屏时还原场景与演出天空，收尾由 End() 统一处理。</summary>
    public void RestoreSceneAndSky()
    {
        if (!m_running)
        {
            return;
        }

        RestoreScene();
        RestoreFogSky();
        RestoreEnemyBars();
    }

    // ===== 目标查找与闪现 =====

    private Enemy FindNearestEnemy()
    {
        if (m_player == null)
        {
            return null;
        }

        return EnemyRegistry.FindNearest(m_player.transform.position, float.PositiveInfinity);
    }

    private void SnapToTarget(Enemy target)
    {
        var stats = m_player.stats != null ? m_player.stats.current : null;
        float standoff = stats != null && stats.finishSkillSnapDistance > 0f
            ? stats.finishSkillSnapDistance
            : k_DefaultSnapDistance;

        var toTarget = target.transform.position - m_player.transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;

        // 已在距离内则只面向敌人
        if (distance <= standoff + 0.01f)
        {
            if (distance > 0.001f)
            {
                m_player.transform.rotation = Quaternion.LookRotation(toTarget / distance, Vector3.up);
            }
            return;
        }

        var direction = toTarget / distance;
        var destination = target.transform.position - direction * standoff;
        destination.y = m_player.transform.position.y;

        // 瞬移前临时关闭控制器，避免被碰撞弹开
        var controller = m_player.controller;
        if (controller != null && controller.enabled)
        {
            controller.enabled = false;
            m_player.transform.position = destination;
            controller.enabled = true;
        }
        else
        {
            m_player.transform.position = destination;
        }

        m_player.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    // ===== 场景隔离：只留角色 =====

    private bool WantIsolateScene()
    {
        var stats = m_player.stats != null ? m_player.stats.current : null;
        return stats == null || stats.finishSkillIsolateScene;
    }

    private void IsolateScene()
    {
        m_hiddenRoots.Clear();

        var roots = m_player.gameObject.scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            var root = roots[i];
            if (root == null || ShouldKeepRoot(root))
            {
                continue;
            }

            m_hiddenRoots.Add(new RootState
            {
                root = root,
                wasActive = root.activeSelf,
            });
            root.SetActive(false);
        }

        if (m_hiddenRoots.Count > 0)
        {
            GameLog.Log($"[FinishSkillDirector] 大招演出隐藏 {m_hiddenRoots.Count} 个根物体，场景中只保留角色。");
        }
    }

    /// <summary>大招期间需要保留的根物体。</summary>
    private bool ShouldKeepRoot(GameObject root)
    {
        if (root == m_playerRoot)
        {
            return true;
        }

        if (m_fogObject != null && GetSceneRoot(m_fogObject.transform) == root)
        {
            return true;
        }

        if (root.GetComponentInChildren<Player>(true) != null) return true;
        if (root.GetComponentInChildren<Camera>(true) != null) return true;
        if (root.GetComponentInChildren<Light>(true) != null) return true;
        if (root.GetComponentInChildren<Canvas>(true) != null) return true;
        if (root.GetComponentInChildren<AudioListener>(true) != null) return true;
        if (root.GetComponentInChildren<Volume>(true) != null) return true;
        if (root.GetComponentInChildren<CinemachineCamera>(true) != null) return true;
        if (root.GetComponentInChildren<CinemachineBrain>(true) != null) return true;

        // 隐藏流程 / 暂停对象可能导致启动或暂停异常
        if (root.GetComponentInChildren<LevelPauser>(true) != null) return true;
        if (root.GetComponentInChildren<LevelManager>(true) != null) return true;
        if (root.GetComponentInChildren<LevelStarter>(true) != null) return true;
        if (root.GetComponentInChildren<BattleBootstrap>(true) != null) return true;

        return false;
    }

    private void RestoreScene()
    {
        for (int i = 0; i < m_hiddenRoots.Count; i++)
        {
            var entry = m_hiddenRoots[i];
            if (entry.root != null)
            {
                entry.root.SetActive(entry.wasActive);
            }
        }

        m_hiddenRoots.Clear();
    }

    // ===== 大招期间隐藏敌人的 HP / 失衡 HUD =====

    private void HideEnemyBars()
    {
        if (m_hiddenEnemyUi.Count > 0)
        {
            return;
        }

        var worldBars = Object.FindObjectsByType<EnemyWorldBar>(FindObjectsSortMode.None);
        for (int i = 0; i < worldBars.Length; i++)
        {
            if (worldBars[i] != null)
            {
                RememberEnemyUi(worldBars[i].gameObject);
            }
        }

        var healthBars = Object.FindObjectsByType<HealthBarUI>(FindObjectsSortMode.None);
        for (int i = 0; i < healthBars.Length; i++)
        {
            var bar = healthBars[i];
            if (bar == null || bar.gameObject == null)
            {
                continue;
            }
            if (bar.gameObject.name.IndexOf("Enemy", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                RememberEnemyUi(bar.gameObject);
            }
        }

        if (m_hiddenEnemyUi.Count > 0)
        {
            GameLog.Log($"[FinishSkillDirector] 大招演出隐藏 {m_hiddenEnemyUi.Count} 个敌人 HUD 根节点。");
        }
    }

    private void RememberEnemyUi(GameObject go)
    {
        if (go == null || !go.activeSelf)
        {
            return;
        }
        for (int i = 0; i < m_hiddenEnemyUi.Count; i++)
        {
            if (m_hiddenEnemyUi[i] == go)
            {
                return;
            }
        }
        m_hiddenEnemyUi.Add(go);
        m_hiddenEnemyUiWasActive.Add(true);
        go.SetActive(false);
    }

    private void RestoreEnemyBars()
    {
        for (int i = 0; i < m_hiddenEnemyUi.Count; i++)
        {
            var go = m_hiddenEnemyUi[i];
            if (go != null && m_hiddenEnemyUiWasActive[i])
            {
                go.SetActive(true);
            }
        }
        m_hiddenEnemyUi.Clear();
        m_hiddenEnemyUiWasActive.Clear();
    }

    private static GameObject GetSceneRoot(Transform transform)
    {
        var current = transform;
        while (current.parent != null)
        {
            current = current.parent;
        }
        return current.gameObject;
    }

    // ===== Height Fog Global 演出天空 =====

    /// <summary>获取 Height Fog Global：场景没有时临时创建，结束后销毁。</summary>
    private void PrepareFogSky()
    {
        if (m_fog != null)
        {
            return;
        }

        var fogs = Object.FindObjectsByType<HeightFogGlobal>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (fogs != null && fogs.Length > 0)
        {
            m_fog = fogs[0];
            m_fogObject = m_fog.gameObject;
            m_fogWasActive = m_fogObject.activeSelf;
        }
        else
        {
            // 临时创建，HeightFogGlobal.Awake 会自动搭好天空球
            var go = new GameObject("Height Fog Global");
            m_fog = go.AddComponent<HeightFogGlobal>();
            m_fogObject = go;
            m_fogCreatedByUs = true;
            m_fogWasActive = true;
            GameLog.Log("[FinishSkillDirector] 场景中未找到 Height Fog Global，已临时创建演出天空。");
        }

        if (m_fogObject != null && !m_fogObject.activeSelf)
        {
            m_fogObject.SetActive(true);
        }

        ApplyDeepBlueFog();
    }

    private void ApplyDeepBlueFog()
    {
        if (m_fog == null)
        {
            return;
        }

        if (!m_fogValuesBackedUp)
        {
            m_fogModeBackup = m_fog.fogMode;
            m_fogColorStartBackup = m_fog.fogColorStart;
            m_fogColorEndBackup = m_fog.fogColorEnd;
            m_fogIntensityBackup = m_fog.fogIntensity;
            m_skyboxFogFillBackup = m_fog.skyboxFogFill;
            m_fogValuesBackedUp = true;
        }

        // 脚本设置模式，避免与 Inspector 配置相互覆盖
        m_fog.fogMode = AtmosphericHeightFog.FogMode.UseScriptSettings;
        m_fog.fogColorStart = k_DeepBlueSkyStart;
        m_fog.fogColorEnd = k_DeepBlueSkyEnd;
        m_fog.fogIntensity = 1f;
        m_fog.skyboxFogFill = 1f;
    }

    private void RestoreFogSky()
    {
        if (m_fog != null && m_fogValuesBackedUp)
        {
            m_fog.fogMode = m_fogModeBackup;
            m_fog.fogColorStart = m_fogColorStartBackup;
            m_fog.fogColorEnd = m_fogColorEndBackup;
            m_fog.fogIntensity = m_fogIntensityBackup;
            m_fog.skyboxFogFill = m_skyboxFogFillBackup;
        }

        m_fogValuesBackedUp = false;

        if (m_fogCreatedByUs)
        {
            if (m_fogObject != null)
            {
                Object.Destroy(m_fogObject);
            }
        }
        else if (m_fogObject != null && !m_fogWasActive)
        {
            m_fogObject.SetActive(false);
        }

        m_fog = null;
        m_fogObject = null;
        m_fogCreatedByUs = false;
        m_fogWasActive = false;
    }

    // ===== 命中结算 =====

    private void TryDealHit(float normalizedTime)
    {
        if (m_hitDealt || normalizedTime < 0f)
        {
            return;
        }

        var stats = m_player.stats != null ? m_player.stats.current : null;
        float hitTime = stats != null ? stats.finishSkillHitTime : k_DefaultHitTime;
        if (normalizedTime < Mathf.Clamp01(hitTime))
        {
            return;
        }

        m_hitDealt = true;

        if (m_target.isDead)
        {
            return;
        }

        // 与敌人最大生命比例取较大值，保证高血量敌人也能看到明显伤害
        float fixedDamage = m_player.GetSkillAttackDamage(Player_SkillState.SkillAction.FinishSkill);
        float maxHpRatioDamage = m_target.maxHp
            * (stats != null ? Mathf.Max(0f, stats.finishSkillDamageRatio) : 0f);
        float damage = Mathf.Max(fixedDamage, maxHpRatioDamage);
        float finalDamage = m_player.RollDamage(damage, out bool isCrit);
        if (finalDamage <= 0f)
        {
            return;
        }

        m_target.TakeUltimateDamage(finalDamage);
        var damagePool = FloatingDamagePool.Ensure();
        if (damagePool != null)
        {
            damagePool.Spawn(m_target.GetHeadPosition(0.35f), finalDamage, isCrit, false);
        }
        m_player.PlayHitImpulse(stats != null ? stats.hitShakeForce : 1f);

        GameLog.Log($"[FinishSkillDirector] 大招命中 {m_target.name}：伤害 {finalDamage:0.##}，HP {m_target.currentHp:0.##}/{m_target.maxHp:0.##}");
    }
}
