using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 角色图鉴窗口：按本地存档刷新角色槽位与立绘，
/// 槽位命名 Slot_0 ~ Slot_11，对应角色目录下标。
/// </summary>
public class CodexPanel : MonoBehaviour
{
    private GameObject[] m_slots;
    private Image[] m_portraitImages;
    private Text m_title;
    private Text m_emptyHint;

    private void Awake()
    {
        CacheSlots();
        EnsureSlotPortraits();
        m_title = FindPathText(transform, "CodexBg/CodexTitle");
        m_emptyHint = FindPathText(transform, "CodexBg/EmptyHint");
    }

    /// <summary>取节点上的 Text，没有则取子物体中的 Text。</summary>
    private static Text FindPathText(Transform root, string path)
    {
        var node = root != null ? root.Find(path) : null;
        if (node == null)
        {
            return null;
        }
        var text = node.GetComponent<Text>();
        return text != null ? text : node.GetComponentInChildren<Text>(true);
    }

    private static Text FindChildText(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }
        var node = parent.Find(childName);
        if (node == null)
        {
            return null;
        }
        var text = node.GetComponent<Text>();
        return text != null ? text : node.GetComponentInChildren<Text>(true);
    }

    private void CacheSlots()
    {
        int total = CharacterCatalog.all.Count;
        m_slots = new GameObject[total];
        var slotsRoot = transform.Find("CodexBg/Slots");
        if (slotsRoot == null)
        {
            Debug.LogWarning("[CodexPanel] 未找到 Slots 容器");
            return;
        }

        for (int i = 0; i < total; i++)
        {
            var slot = slotsRoot.Find("Slot_" + i);
            if (slot != null)
            {
                m_slots[i] = slot.gameObject;
            }
        }
    }

    /// <summary>确保每个槽位都有 Portrait 立绘节点。</summary>
    public int EnsureSlotPortraits()
    {
        if (m_slots == null)
        {
            CacheSlots();
        }
        if (m_slots == null)
        {
            return 0;
        }

        if (m_portraitImages == null || m_portraitImages.Length != m_slots.Length)
        {
            m_portraitImages = new Image[m_slots.Length];
        }

        int ensured = 0;
        for (int i = 0; i < m_slots.Length; i++)
        {
            if (m_slots[i] == null)
            {
                continue;
            }

            var portrait = FindOrCreateSlotPortrait(m_slots[i].transform);
            if (portrait != null)
            {
                m_portraitImages[i] = portrait;
                ensured++;
            }
        }
        return ensured;
    }

    private Image FindOrCreateSlotPortrait(Transform slot)
    {
        var node = slot.Find("Portrait");
        if (node != null)
        {
            return node.GetComponent<Image>();
        }

        var slotRect = slot as RectTransform;
        var go = new GameObject("Portrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = slot.gameObject.layer;

        var rect = (RectTransform)go.transform;
        rect.SetParent(slot, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        Vector2 slotSize = slotRect != null
            ? new Vector2(slotRect.rect.width, slotRect.rect.height)
            : new Vector2(280f, 230f);
        rect.sizeDelta = slotSize;

        var image = go.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.92f);
        image.preserveAspect = true;
        image.raycastTarget = false;
        rect.SetAsFirstSibling(); // 画在 Rarity / Name 文字下方
        return image;
    }

    public void Refresh()
    {
        if (m_slots == null) return;

        int owned = PlayerInventory.Count;
        if (m_title != null)
        {
            m_title.text = $"已拥有角色图鉴  ({owned}/{CharacterCatalog.all.Count})";
        }
        if (m_emptyHint != null)
        {
            m_emptyHint.gameObject.SetActive(owned == 0);
        }

        var ownedIds = PlayerInventory.owned;
        for (int i = 0; i < CharacterCatalog.all.Count; i++)
        {
            if (m_slots[i] == null) continue;
            var def = CharacterCatalog.all[i];
            bool has = ownedIds.Contains(def.id);
            m_slots[i].SetActive(has);
            if (!has) continue;

            var rarityText = FindChildText(m_slots[i].transform, "Rarity");
            var nameText = FindChildText(m_slots[i].transform, "Name");
            if (rarityText != null)
            {
                rarityText.text = def.RarityLabel;
                rarityText.color = def.RarityColor;
            }
            if (nameText != null)
            {
                nameText.text = def.name;
            }

            // 槽位底色按稀有度轻微染色
            var bg = m_slots[i].GetComponent<Image>();
            if (bg != null)
            {
                Color c = def.RarityColor;
                bg.color = new Color(c.r * 0.18f, c.g * 0.18f, c.b * 0.22f, 0.95f);
            }

            if (m_portraitImages != null && i < m_portraitImages.Length && m_portraitImages[i] != null)
            {
                m_portraitImages[i].sprite = GachaArt.GetByCharacter(def);
            }
        }

        if (LobbyUI.instance != null)
        {
            LobbyUI.instance.RefreshProgress();
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}
