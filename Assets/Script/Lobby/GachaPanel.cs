using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 抽卡窗口：抽取角色并展示结果卡片，
/// 结果立绘缺失时按需创建，可由编辑器菜单生成持久节点。
/// </summary>
public class GachaPanel : MonoBehaviour
{
    private GameObject m_resultRoot;
    private Image m_resultStrip;
    private Image m_resultPortrait;
    private Text m_resultRarity;
    private Text m_resultName;
    private Text m_resultTag;
    private Text m_resultSub;
    private Text m_resultDesc;
    private Text m_ownedText;

    private void Awake()
    {
        var bg = transform.Find("GachaBg");
        if (bg != null)
        {
            m_ownedText = FindChildText(bg, "OwnedText");
        }

        CacheResultRefs();
    }

    /// <summary>取节点上的 Text，没有则取子物体中的 Text。</summary>
    private static Text FindChildText(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }
        var node = root.Find(childName);
        if (node == null)
        {
            return null;
        }
        var text = node.GetComponent<Text>();
        return text != null ? text : node.GetComponentInChildren<Text>(true);
    }

    /// <summary>结果弹窗是画布下的隐藏兄弟节点，从父节点缓存引用。</summary>
    private void CacheResultRefs()
    {
        m_resultRoot = transform.parent != null
            ? transform.parent.Find("GachaResultRoot")?.gameObject
            : null;
        if (m_resultRoot == null)
        {
            return;
        }

        var panel = m_resultRoot.transform.Find("ResultPanel");
        if (panel == null)
        {
            return;
        }

        m_resultStrip = panel.Find("ResultStrip")?.GetComponent<Image>();
        m_resultRarity = FindChildText(panel, "RarityText");
        m_resultName = FindChildText(panel, "NameText");
        m_resultTag = FindChildText(panel, "TagText");
        m_resultSub = FindChildText(panel, "SubText");
        m_resultDesc = FindChildText(panel, "DescText");
        m_resultPortrait = FindOrCreatePortrait(panel);
    }

    /// <summary>确保 ResultPanel 下存在 Portrait 节点。</summary>
    public Image EnsureResultPortrait()
    {
        if (m_resultRoot == null)
        {
            CacheResultRefs();
        }
        return m_resultPortrait;
    }

    private Image FindOrCreatePortrait(Transform panel)
    {
        var node = panel.Find("Portrait");
        if (node != null)
        {
            return node.GetComponent<Image>();
        }

        var go = new GameObject("Portrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = panel.gameObject.layer;

        var rect = (RectTransform)go.transform;
        rect.SetParent(panel, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(500f, 640f);

        var image = go.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.72f);
        image.preserveAspect = true;
        image.raycastTarget = false;
        rect.SetAsFirstSibling();
        return image;
    }

    public void RefreshInfo()
    {
        if (m_ownedText != null)
        {
            m_ownedText.text = $"图鉴收集：{PlayerInventory.Count} / {CharacterCatalog.all.Count}";
        }
        if (LobbyUI.instance != null)
        {
            LobbyUI.instance.RefreshProgress();
        }
    }

    // ===== 场景按钮绑定 =====

    public void Pull()
    {
        CharacterDef def = GachaSystem.Pull();
        bool isNew = PlayerInventory.Add(def.id);

        Color color = def.RarityColor;
        if (m_resultStrip != null) m_resultStrip.color = color;
        if (m_resultRarity != null)
        {
            m_resultRarity.text = def.RarityLabel;
            m_resultRarity.color = color;
        }
        if (m_resultName != null) m_resultName.text = def.name;
        if (m_resultSub != null) m_resultSub.text = def.title;
        if (m_resultDesc != null) m_resultDesc.text = def.description;
        if (m_resultTag != null)
        {
            m_resultTag.text = isNew ? "首次获得 · 已收录进图鉴" : "重复角色";
            m_resultTag.color = isNew ? new Color(0.45f, 1f, 0.6f) : new Color(1f, 0.83f, 0.3f);
        }

        if (m_resultPortrait != null)
        {
            m_resultPortrait.sprite = GachaArt.GetByCharacter(def);
        }

        if (m_resultRoot != null) m_resultRoot.SetActive(true);
        RefreshInfo();

        var codex = FindObjectOfType<CodexPanel>();
        codex?.Refresh();
    }

    public void HideResult()
    {
        if (m_resultRoot != null) m_resultRoot.SetActive(false);
    }

    public void ResetProgress()
    {
        PlayerInventory.Reset();
        RefreshInfo();
        var codex = FindObjectOfType<CodexPanel>();
        codex?.Refresh();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        RefreshInfo();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}
