using UnityEngine;

/// <summary>
/// 抽卡角色立绘：从 Resources/抽卡素材 按固定顺序加载，
/// 文件名顺序对应角色目录下标。
/// </summary>
public static class GachaArt
{
    private const string k_Folder = "抽卡素材";

    // 资源名顺序与 CharacterCatalog.all 保持一致
    private static readonly string[] k_OrderedFileNames =
    {
        "11号",
        "仪玄",
        "千夏",
        "叶瞬光",
        "比利",
        "派派",
        "爱丽丝",
        "爱芮",
        "简杜",
        "维琳娜",
        "铃",
        "露西",
    };

    private static Sprite[] s_sprites;
    private static bool s_tried;

    /// <summary>按角色目录下标取立绘。</summary>
    public static Sprite GetByCharacterIndex(int index)
    {
        EnsureLoaded();
        if (s_sprites == null || index < 0 || index >= s_sprites.Length)
        {
            return null;
        }
        return s_sprites[index];
    }

    /// <summary>按角色定义取立绘，找不到返回 null。</summary>
    public static Sprite GetByCharacter(CharacterDef def)
    {
        if (def == null)
        {
            return null;
        }

        var all = CharacterCatalog.all;
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].id == def.id)
            {
                return GetByCharacterIndex(i);
            }
        }
        return null;
    }

    private static void EnsureLoaded()
    {
        if (s_tried)
        {
            return;
        }
        s_tried = true;

        s_sprites = new Sprite[CharacterCatalog.all.Count];
        int loaded = 0;
        for (int i = 0; i < k_OrderedFileNames.Length && i < s_sprites.Length; i++)
        {
            var sprite = Resources.Load<Sprite>(k_Folder + "/" + k_OrderedFileNames[i]);
            if (sprite == null)
            {
                Debug.LogWarning("[GachaArt] 找不到资源 " + k_OrderedFileNames[i]
                    + "（请确认文件在 Resources/抽卡素材 下且导入类型为 Sprite）。");
                continue;
            }
            s_sprites[i] = sprite;
            loaded++;
        }

        if (loaded == 0)
        {
            Debug.LogWarning("[GachaArt] 抽卡素材加载失败：Resources/抽卡素材 下没有可用的 Sprite。");
        }
        else if (loaded != CharacterCatalog.all.Count)
        {
            Debug.LogWarning($"[GachaArt] 抽卡素材只加载了 {loaded}/{CharacterCatalog.all.Count} 张，请检查文件数量。");
        }
    }
}
