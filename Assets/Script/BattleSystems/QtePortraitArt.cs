using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// QTE 选人立绘：从 QtePortraits 资源目录按名称加载并缓存。
/// </summary>
public static class QtePortraitArt
{
    private const string k_PortraitPath = "QtePortraits/";
    private static readonly Dictionary<string, Sprite> m_cache = new Dictionary<string, Sprite>();
    private static Sprite m_ringSprite;
    private static Sprite m_defaultSprite;
    private static bool m_defaultLogged;

    /// <summary>统一头像：11号。</summary>
    public static Sprite defaultSprite
    {
        get
        {
            if (m_defaultSprite == null && !m_defaultLogged)
            {
                var sprites = Resources.LoadAll<Sprite>(k_PortraitPath + "11号");
                m_defaultSprite = sprites != null && sprites.Length > 0 ? sprites[0] : null;
                if (m_defaultSprite == null)
                {
                    m_defaultLogged = true;
                    Debug.LogWarning("[QtePortraitArt] 找不到统一头像资源 QtePortraits/11号，回退原立绘/色块。");
                }
            }
            return m_defaultSprite;
        }
    }

    /// <summary>头像外围圆环框。</summary>
    public static Sprite ringSprite
    {
        get
        {
            if (m_ringSprite == null)
            {
                var sprites = Resources.LoadAll<Sprite>(k_PortraitPath + "Ring");
                m_ringSprite = sprites != null && sprites.Length > 0 ? sprites[0] : null;
            }
            return m_ringSprite;
        }
    }

    /// <summary>按显示名查找立绘。</summary>
    public static Sprite GetFor(string displayName)
    {
        // 当前统一使用 11号
        if (defaultSprite != null)
        {
            return defaultSprite;
        }

        if (string.IsNullOrEmpty(displayName))
        {
            return null;
        }

        // 11号缺失时按角色名回退
        string normalized = displayName.ToLowerInvariant();
        string assetName = null;
        if (normalized.Contains("anby") || normalized.Contains("安比"))
        {
            assetName = "AnBi";
        }
        else if (normalized.Contains("billy") || normalized.Contains("bili") || normalized.Contains("比利"))
        {
            assetName = "BiLi";
        }
        else if (normalized.Contains("corin") || normalized.Contains("kelin") || normalized.Contains("可琳"))
        {
            assetName = "KeLin";
        }

        if (assetName == null)
        {
            return null;
        }
        return Load(assetName);
    }

    private static Sprite Load(string assetName)
    {
        if (m_cache.TryGetValue(assetName, out var cached))
        {
            return cached;
        }

        // Multiple-Sprite 素材取第一张子图
        var sprites = Resources.LoadAll<Sprite>(k_PortraitPath + assetName);
        Sprite result = sprites != null && sprites.Length > 0 ? sprites[0] : null;
        m_cache[assetName] = result;
        if (result == null)
        {
            Debug.LogWarning("[QtePortraitArt] 找不到立绘资源 " + k_PortraitPath + assetName
                + "（请确认文件已拷入 Resources/QtePortraits）。");
        }
        return result;
    }
}
