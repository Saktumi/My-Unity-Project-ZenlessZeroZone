using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>角色稀有度。</summary>
public enum CharacterRarity
{
    R,
    SR,
    SSR,
}

/// <summary>一个可抽取/可收集角色的静态定义。</summary>
[Serializable]
public class CharacterDef
{
    public string id;
    public string name;
    public CharacterRarity rarity;
    public string title;        // 称号
    public string description;  // 一句话介绍

    public CharacterDef(string id, string name, CharacterRarity rarity, string title, string description)
    {
        this.id = id;
        this.name = name;
        this.rarity = rarity;
        this.title = title;
        this.description = description;
    }

    public string RarityLabel => rarity.ToString();

    public Color RarityColor => GetRarityColor(rarity);

    public static Color GetRarityColor(CharacterRarity rarity)
    {
        switch (rarity)
        {
            case CharacterRarity.SSR: return new Color(1f, 0.79f, 0.30f); // 金
            case CharacterRarity.SR:  return new Color(0.72f, 0.42f, 1f);  // 紫
            default:                  return new Color(0.36f, 0.65f, 1f);  // 蓝
        }
    }
}

/// <summary>角色卡池静态表；顺序与 GachaArt.k_OrderedFileNames 一一对应。</summary>
public static class CharacterCatalog
{
    public static readonly List<CharacterDef> all = new List<CharacterDef>
    {
        // SSR
        new CharacterDef("soldier11", "11号", CharacterRarity.SSR, "完美士兵",
            "新艾利都防卫军奥波勒斯小队的火属性强攻，服从指令、忠于使命，冷静而高效。"),
        new CharacterDef("yixuan", "仪玄", CharacterRarity.SSR, "宗师",
            "玄墨属性命破代理人，能无视防御造成贯穿伤害，虚狩级战力的云岿山宗师。"),
        new CharacterDef("qianxia", "千夏", CharacterRarity.SSR, "作曲人",
            "妄想天使组合的作曲人，物理属性支援，能为全队附加天使协律并开启以太帷幕。"),
        new CharacterDef("yeshuguang", "叶瞬光", CharacterRarity.SSR, "青溟剑主",
            "云岿山青溟剑的剑主，凛刃属性强攻，可进入明心境开启以太帷幕·决裁爆发。"),
        // SR
        new CharacterDef("billy", "比利", CharacterRarity.SR, "星徽骑士",
            "狡兔屋的智能机械人，自称星徽骑士，手持故人赠送的双枪「姑娘们」的强攻手。"),
        new CharacterDef("piper", "派派", CharacterRarity.SR, "货车司机",
            "卡吕冬之子的司机，烟嗓随和，用连续旋斩快速累积物理异常的异常角色。"),
        // SSR
        new CharacterDef("alice", "爱丽丝", CharacterRarity.SSR, "名门继承人",
            "泰姆菲尔德家族的优雅继承人，擅长击剑、厌恶不对称，物理属性异常代理人。"),
        new CharacterDef("aria", "爱芮", CharacterRarity.SSR, "妄想天使主唱",
            "妄想天使组合的主唱与门面，以太属性异常输出，蓄力「绝对音准」带来爆发伤害。"),
        new CharacterDef("jane_doe", "简杜", CharacterRarity.SSR, "刑侦特勤",
            "刑侦特勤队的物理异常王牌，身法灵活，快速触发强击并擅长闪避反击。"),
        new CharacterDef("velina", "维琳娜", CharacterRarity.SSR, "外务总务官",
            "罗斯凯利法外务筹策局的总务官，以折扇为武器的风属性异常代理人。"),
        // SSR（主角）
        new CharacterDef("ling", "铃", CharacterRarity.SSR, "法厄同",
            "Random Play 的店长之一，新艾利都传说级的绳匠「法厄同」，元气机敏的引路人。"),
        // SR
        new CharacterDef("lucy", "露西", CharacterRarity.SR, "卡吕冬之子",
            "红发金眼的卡吕冬之子成员，火属性支援，用亲卫队小猪为全队提供增益。"),
    };

    public static CharacterDef GetById(string id)
    {
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].id == id) return all[i];
        }
        return null;
    }
}

/// <summary>抽卡逻辑：按稀有度权重随机出一个角色。</summary>
public static class GachaSystem
{
    // SSR / SR / R 的权重
    private const float SsrWeight = 5f;
    private const float SrWeight = 25f;
    private const float RWeight = 70f;

    public static CharacterDef Pull()
    {
        CharacterRarity rarity = RollRarity();
        var pool = new List<CharacterDef>();
        for (int i = 0; i < CharacterCatalog.all.Count; i++)
        {
            if (CharacterCatalog.all[i].rarity == rarity)
            {
                pool.Add(CharacterCatalog.all[i]);
            }
        }

        if (pool.Count == 0)
        {
            return CharacterCatalog.all[0];
        }
        return pool[UnityEngine.Random.Range(0, pool.Count)];
    }

    private static CharacterRarity RollRarity()
    {
        // 只统计池中真实存在的稀有度，避免空档回退成固定角色
        bool hasSsr = false;
        bool hasSr = false;
        bool hasR = false;
        for (int i = 0; i < CharacterCatalog.all.Count; i++)
        {
            switch (CharacterCatalog.all[i].rarity)
            {
                case CharacterRarity.SSR: hasSsr = true; break;
                case CharacterRarity.SR: hasSr = true; break;
                default: hasR = true; break;
            }
        }

        float total = 0f;
        if (hasSsr) total += SsrWeight;
        if (hasSr) total += SrWeight;
        if (hasR) total += RWeight;

        float roll = UnityEngine.Random.value * total;
        if (hasSsr)
        {
            if (roll < SsrWeight) return CharacterRarity.SSR;
            roll -= SsrWeight;
        }
        if (hasSr)
        {
            if (roll < SrWeight) return CharacterRarity.SR;
            roll -= SrWeight;
        }
        return CharacterRarity.R;
    }
}

/// <summary>本地存档用的包装类。</summary>
[Serializable]
public class CharacterSaveData
{
    public List<string> ids = new List<string>();
}

/// <summary>
/// 玩家已拥有角色（本地存档）。抽出的角色写入这里，图鉴读取这里。
/// </summary>
public static class PlayerInventory
{
    public const string SaveKey = "Lobby_OwnedCharacters_v1";

    private static List<string> m_owned;

    public static List<string> owned
    {
        get
        {
            EnsureLoaded();
            return m_owned;
        }
    }

    public static bool Has(string id)
    {
        EnsureLoaded();
        return m_owned.Contains(id);
    }

    /// <summary>添加角色，返回是否为首次获得（新角色）。</summary>
    public static bool Add(string id)
    {
        EnsureLoaded();
        bool isNew = !m_owned.Contains(id);
        if (isNew)
        {
            m_owned.Add(id);
            Save();
        }
        return isNew;
    }

    public static int Count
    {
        get
        {
            EnsureLoaded();
            return m_owned.Count;
        }
    }

    public static void Save()
    {
        EnsureLoaded();
        var data = new CharacterSaveData { ids = m_owned };
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public static void Load()
    {
        m_owned = new List<string>();
        string json = PlayerPrefs.GetString(SaveKey, string.Empty);
        if (string.IsNullOrEmpty(json)) return;

        try
        {
            var data = JsonUtility.FromJson<CharacterSaveData>(json);
            if (data != null && data.ids != null)
            {
                // 丢弃不属于当前图鉴的旧存档 id，保证计数与槽位一致
                m_owned = new List<string>();
                for (int i = 0; i < data.ids.Count; i++)
                {
                    string id = data.ids[i];
                    if (CharacterCatalog.GetById(id) != null)
                    {
                        m_owned.Add(id);
                    }
                }
                if (m_owned.Count != data.ids.Count)
                {
                    Save();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PlayerInventory] 读取存档失败，已重置: {e.Message}");
            m_owned = new List<string>();
        }
    }

    /// <summary>清空存档（调试用）。</summary>
    public static void Reset()
    {
        m_owned = new List<string>();
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }

    private static void EnsureLoaded()
    {
        if (m_owned == null)
        {
            Load();
        }
    }
}
