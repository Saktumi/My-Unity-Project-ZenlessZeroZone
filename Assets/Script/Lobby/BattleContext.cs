using System.Collections.Generic;
using UnityEngine;

/// <summary>怪物难度档位。</summary>
public enum MonsterTier
{
    Normal,
    Elite,
    Berserk,
}

/// <summary>战斗怪物的静态定义。</summary>
public class MonsterDef
{
    public string id;
    public string name;
    public MonsterTier tier;
    public string description;
    public Color tint;

    // 数值覆盖
    public float detectRadius;
    public float attackRange;
    public float cooldownMin;
    public float cooldownMax;
    public float topSpeed;
    public float rotationSpeed;

    public int DifficultyStars
    {
        get
        {
            switch (tier)
            {
                case MonsterTier.Elite: return 2;
                case MonsterTier.Berserk: return 3;
                default: return 1;
            }
        }
    }
}

/// <summary>大厅里的可选怪物清单。</summary>
public static class MonsterCatalog
{
    public static readonly List<MonsterDef> all = new List<MonsterDef>
    {
        new MonsterDef
        {
            id = "goblin_normal",
            name = "普通哥布林",
            tier = MonsterTier.Normal,
            description = "游荡的空洞底层怪物，反应平平，适合热身。",
            tint = new Color(0.65f, 0.85f, 0.45f),
            detectRadius = 12f,
            attackRange = 2.5f,
            cooldownMin = 1f,
            cooldownMax = 3f,
            topSpeed = 4f,
            rotationSpeed = 720f,
        },
        new MonsterDef
        {
            id = "goblin_elite",
            name = "精英哥布林",
            tier = MonsterTier.Elite,
            description = "被以太侵蚀得更深，攻击更快、索敌更远。",
            tint = new Color(1f, 0.35f, 0.25f),
            detectRadius = 15f,
            attackRange = 2.8f,
            cooldownMin = 0.7f,
            cooldownMax = 2.2f,
            topSpeed = 5f,
            rotationSpeed = 900f,
        },
        new MonsterDef
        {
            id = "goblin_berserk",
            name = "狂暴哥布林",
            tier = MonsterTier.Berserk,
            description = "完全狂暴化的危险个体，几乎不给你喘息的机会。",
            tint = new Color(0.75f, 0.35f, 1f),
            detectRadius = 18f,
            attackRange = 3f,
            cooldownMin = 0.4f,
            cooldownMax = 1.7f,
            topSpeed = 6f,
            rotationSpeed = 1080f,
        },
    };
}

/// <summary>
/// 跨场景的战斗上下文：大厅选择怪物后写入，SampleScene 的 BattleBootstrap 读取。
/// </summary>
public static class BattleContext
{
    public static int selectedIndex = 0;

    public static MonsterDef selected
    {
        get
        {
            if (selectedIndex < 0 || selectedIndex >= MonsterCatalog.all.Count)
            {
                selectedIndex = 0;
            }
            return MonsterCatalog.all[selectedIndex];
        }
    }

    public static void Select(int index)
    {
        selectedIndex = Mathf.Clamp(index, 0, MonsterCatalog.all.Count - 1);
    }
}
