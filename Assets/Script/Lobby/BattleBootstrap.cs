using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 战斗场景启动器：按大厅选择替换敌人数值并染色，
/// 接线打击反馈与队伍系统，创建战斗 HUD 与结算流程。
/// </summary>
public class BattleBootstrap : MonoBehaviour
{
    [Header("中文字体源（萝莉体.ttf）")]
    [SerializeField] private Font sourceFont;

    [Header("战斗系统（自动接线）")]
    [Tooltip("启用队伍系统：R 切人 + 破防 QTE 连携")]
    public bool enableTeamSystem = true;
    [Tooltip("运行时克隆当前角色组成队伍")]
    public bool autoCloneDemoTeam = false;
    [Tooltip("用世界头顶血条替代右上角敌人角标")]
    public bool useWorldEnemyBars = true;
    [Tooltip("队伍成员染色")]
    public Color[] demoTeamColors;

    private EnemyStats m_appliedStats;

    private const float k_HudMargin = 24f;
    private const float k_HudWidth = 340f;
    private const float k_HudHeight = 26f;

    private void Start()
    {
        Time.timeScale = 1f;
        EnsureLabelFonts();
        // 等一帧，确保各组件 Start 先执行完毕
        StartCoroutine(SetupBattleLater());
    }

    /// <summary>战斗 UI 字体：中文用萝莉体，纯英文用内置字体。</summary>
    private void EnsureLabelFonts()
    {
        var texts = UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None);
        if (texts == null || texts.Length == 0)
        {
            return;
        }

        Font asciiFont = null;
        foreach (var text in texts)
        {
            if (text == null || string.IsNullOrEmpty(text.text))
            {
                continue;
            }

            bool asciiOnly = true;
            foreach (char ch in text.text)
            {
                if (ch >= 128)
                {
                    asciiOnly = false;
                    break;
                }
            }

            if (!asciiOnly)
            {
                if (sourceFont != null)
                {
                    text.font = sourceFont;
                }
                continue;
            }

            // 纯英文切到内置字体，避免缺字形显示空白
            if (asciiFont == null)
            {
                asciiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (asciiFont == null)
                {
                    asciiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
            }
            if (asciiFont != null)
            {
                text.font = asciiFont;
            }
        }
    }

    private IEnumerator SetupBattleLater()
    {
        yield return null;
        LevelManager.LockCursor(false);

        // 打击反馈：顿帧 / 慢动作与相机震屏
        EnsureFeedbackSystems();

        // 应用大厅选择的怪物配置
        ApplyMonster(BattleContext.selected);

        // 队伍系统：R 切人与 QTE 连携
        var scenePlayer = UnityEngine.Object.FindFirstObjectByType<Player>();
        if (scenePlayer != null)
        {
            EnsureTeamSystem(scenePlayer);
        }

        // 战斗 HUD：角色血条与敌人血条
        BuildBattleHud();
        if (useWorldEnemyBars)
        {
            var enemies = UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            EnsureWorldBars(enemies);
        }

        // 战斗结算：胜利 / 失败与重开
        EnsureBattleFlow();
    }

    private void EnsureFeedbackSystems()
    {
        if (GetComponent<HitFeelManager>() == null)
        {
            gameObject.AddComponent<HitFeelManager>();
        }
        // 相机震屏器挂到主相机
        var mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.GetComponent<CameraShakeManager>() == null)
        {
            mainCamera.gameObject.AddComponent<CameraShakeManager>();
        }
    }

    private void EnsureTeamSystem(Player player)
    {
        if (!enableTeamSystem)
        {
            return;
        }
        var team = GetComponent<CharacterSwitcher>();
        if (team == null)
        {
            team = gameObject.AddComponent<CharacterSwitcher>();
        }
        team.autoCloneTeam = autoCloneDemoTeam;
        if (demoTeamColors != null && demoTeamColors.Length > 0)
        {
            team.demoMemberColors = demoTeamColors;
        }
        team.SetupBattle(player);
    }

    private void EnsureWorldBars(Enemy[] enemies)
    {
        if (enemies == null || enemies.Length == 0)
        {
            return;
        }
        var layer = GetComponent<WorldBarLayer>();
        if (layer == null)
        {
            layer = gameObject.AddComponent<WorldBarLayer>();
        }
        layer.BuildAll(enemies);
    }

    private void EnsureBattleFlow()
    {
        var flow = GetComponent<BattleFlowController>();
        if (flow == null)
        {
            flow = gameObject.AddComponent<BattleFlowController>();
        }
        flow.Setup(sourceFont);
    }

    private void OnDestroy()
    {
        // 释放运行时创建的属性对象
        if (m_appliedStats != null)
        {
            Destroy(m_appliedStats);
            m_appliedStats = null;
        }
    }

    private void ApplyMonster(MonsterDef def)
    {
        if (def == null)
        {
            return;
        }

        var enemies = UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        if (enemies == null || enemies.Length == 0)
        {
            Debug.LogWarning("[BattleBootstrap] 场景中未找到 Enemy");
            return;
        }

        var enemy = enemies[0];
        var statsManager = enemy.stats;
        if (statsManager == null || statsManager.stats == null || statsManager.stats.Length == 0)
        {
            Debug.LogWarning("[BattleBootstrap] 敌人没有可用的 EnemyStats");
            return;
        }

        // 复制基础属性再覆盖数值，避免修改原始资产
        string json = JsonUtility.ToJson(statsManager.stats[0]);
        m_appliedStats = ScriptableObject.CreateInstance<EnemyStats>();
        JsonUtility.FromJsonOverwrite(json, m_appliedStats);
        m_appliedStats.name = def.name;

        m_appliedStats.detectRadius = def.detectRadius;
        m_appliedStats.attackRange = def.attackRange;
        m_appliedStats.attackCooldownMin = def.cooldownMin;
        m_appliedStats.attackCooldownMax = def.cooldownMax;
        m_appliedStats.topSpeed = def.topSpeed;
        m_appliedStats.rotationSpeed = def.rotationSpeed;

        statsManager.SetCurrent(m_appliedStats);
        TintEnemy(enemy, def.tint);
        SetTitle(def);

        GameLog.Log($"[BattleBootstrap] 已应用怪物 [{def.name}] 到敌人，难度 {def.DifficultyStars} 星");
    }

    private void TintEnemy(Enemy enemy, Color tint)
    {
        if (enemy == null || tint == default)
        {
            return;
        }

        var renderers = enemy.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            // .material 自动实例化，避免污染共享资产
            Color c = renderers[i].material.color;
            renderers[i].material.color = new Color(c.r * tint.r, c.g * tint.g, c.b * tint.b, c.a);
        }
    }

    private void SetTitle(MonsterDef def)
    {
        BuildTargetBanner(def);
    }

    // ===== 战斗 HUD：角色 / 敌人血条 =====

    private void BuildBattleHud()
    {
        var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[BattleBootstrap] 场景中未找到 Canvas，无法创建血条");
            return;
        }

        var player = UnityEngine.Object.FindFirstObjectByType<Player>();
        if (player != null)
        {
            // 左上角色血条
            BuildHealthBar(canvas, "PlayerHealthBar",
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Color(0.32f, 0.92f, 0.55f, 1f),
                () => ResolveActiveBattlePlayer(player).currentHp,
                () => ResolveActiveBattlePlayer(player).maxHp);
        }
        else
        {
            Debug.LogWarning("[BattleBootstrap] 场景中未找到 Player，无法创建角色血条");
        }

        var enemy = UnityEngine.Object.FindFirstObjectByType<Enemy>();
        if (enemy != null)
        {
            if (!useWorldEnemyBars)
            {
                // 右上角敌人血条
                BuildHealthBar(canvas, "EnemyHealthBar",
                    new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Color(1f, 0.42f, 0.34f, 1f),
                    () => enemy.currentHp, () => enemy.maxHp);
            }
        }
        else
        {
            Debug.LogWarning("[BattleBootstrap] 场景中未找到 Enemy，无法创建敌人血条");
        }

        GameLog.Log("[BattleBootstrap] 战斗 HUD 创建完成：左上角色血条" + (useWorldEnemyBars ? " + 敌人世界头顶血条。" : " + 右上敌人血条。"));
    }

    /// <summary>返回当前受控成员，未启用队伍系统时返回原始玩家。</summary>
    private static Player ResolveActiveBattlePlayer(Player fallback)
    {
        var team = CharacterSwitcher.instance;
        if (team != null && team.activePlayer != null)
        {
            return team.activePlayer;
        }
        return fallback;
    }

    /// <summary>创建一条血条：底色 + 按血量比例填充。</summary>
    private void BuildHealthBar(Canvas canvas, string barName, Vector2 anchor, Vector2 pivot,
        Color fillColor, Func<float> currentGetter, Func<float> maxGetter)
    {
        var root = new GameObject(barName, typeof(RectTransform), typeof(CanvasGroup));
        root.layer = canvas.gameObject.layer;

        var rootRt = (RectTransform)root.transform;
        rootRt.SetParent(canvas.transform, false);
        rootRt.anchorMin = anchor;
        rootRt.anchorMax = anchor;
        rootRt.pivot = pivot;
        rootRt.anchoredPosition = new Vector2(anchor.x <= 0.5f ? k_HudMargin : -k_HudMargin, -k_HudMargin);
        rootRt.sizeDelta = new Vector2(k_HudWidth, k_HudHeight);

        // 深色底
        CreateHudImage(rootRt, "Background", new Color(0.06f, 0.08f, 0.11f, 0.85f));

        // 血条填充：宽度 = 血量比例 × 满血宽度
        var fillImage = CreateHudImage(rootRt, "Fill", fillColor);
        var fillRt = (RectTransform)fillImage.transform;
        float fillBaseWidth = k_HudWidth - 4f;
        float fillHeight = k_HudHeight - 4f;
        fillRt.anchorMin = new Vector2(0f, 0.5f);
        fillRt.anchorMax = new Vector2(0f, 0.5f);
        fillRt.pivot = new Vector2(0f, 0.5f);
        fillRt.anchoredPosition = new Vector2(2f, 0f);
        fillRt.sizeDelta = new Vector2(fillBaseWidth, fillHeight);

        var bar = root.AddComponent<HealthBarUI>();
        bar.Setup(fillImage, fillBaseWidth, currentGetter, maxGetter);

        // 淡入并缩放出现
        var uiAnimator = root.AddComponent<UIAnimator>();
        uiAnimator.duration = 0.35f;
        uiAnimator.Show();

        // 0.5 秒后强制显示，避免补间异常导致 HUD 不可见
        StartCoroutine(ForceHudVisible(rootRt, root.GetComponent<CanvasGroup>()));
    }

    private IEnumerator ForceHudVisible(RectTransform rect, CanvasGroup group)
    {
        yield return new WaitForSecondsRealtime(0.5f);

        if (rect != null)
        {
            rect.localScale = Vector3.one;
        }
        if (group != null)
        {
            group.alpha = 1f;
        }
    }

    /// <summary>创建一个填满父节点的 UI 图片。</summary>
    private static Image CreateHudImage(RectTransform parent, string childName, Color color)
    {
        var go = new GameObject(childName, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void BuildTargetBanner(MonsterDef def)
    {
        var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        var go = new GameObject("MonsterTargetBanner", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(canvas.transform, false);
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -28f);
        rt.sizeDelta = new Vector2(600f, 54f);

        var text = go.AddComponent<Text>();
        text.font = ResolveFont();
        text.fontSize = 28;
        text.color = new Color(1f, 0.85f, 0.45f);
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.raycastTarget = false;

        string stars = string.Empty;
        for (int s = 0; s < 3; s++)
        {
            stars += s < def.DifficultyStars ? "★" : "☆";
        }
        text.text = $"讨伐目标：{def.name}  ·  难度 {stars}";
    }

    private Font ResolveFont()
    {
        return sourceFont != null ? sourceFont : null;
    }

    private void BackToLobby()
    {
        Time.timeScale = 1f;
        LevelManager.LockCursor(false);
        SceneManager.LoadScene("Lobby");
    }
}
