using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// QTE 时停选人面板：展示左右候补成员与倒计时。
/// 引用留空时按默认样式自动生成，也可在场景中手工搭建后回填引用。
/// </summary>
public class SwitchTimePanel : MonoBehaviour
{
    [Header("引用（留空则运行时自动生成默认面板）")]
    [Tooltip("面板根节点。拖入自己搭的面板后，运行时不会再自动生成")]
    public RectTransform uiRoot;
    public CanvasGroup panelGroup;

    [Header("左右候补成员卡片")]
    public Image leftCardImage;
    public Image rightCardImage;
    public Image leftAvatarImage;
    public Image rightAvatarImage;
    public Text leftNameText;
    public Text rightNameText;

    [Header("倒计时 / 提示")]
    public Text countdownText;
    public Text hintText;

    [Header("自动生成时的默认样式")]
    public bool autoBuildRuntimeFallback = true;
    [Tooltip("整条底栏宽度 / 高度 / 停靠高度")]
    public float panelWidth = 1040f;
    public float panelHeight = 210f;
    public float bottomOffset = 110f;
    public float cardOffsetX = 330f;
    public float cardWidth = 190f;
    public float cardHeight = 200f;
    public float avatarSize = 130f;
    [Tooltip("滑入/滑出速度（越大越快）")]
    public float slideSpeed = 12f;
    public Color panelBackdrop = new Color(0.03f, 0.04f, 0.08f, 0.72f);
    public Color cardBackdrop = new Color(0.15f, 0.17f, 0.24f, 0.9f);
    public Color titleColor = new Color(1f, 0.88f, 0.55f, 1f);
    public Color nameColor = new Color(0.92f, 0.94f, 1f, 1f);
    public Color hintColor = new Color(0.8f, 0.85f, 1f, 1f);
    public int titleFontSize = 18;
    public int countdownFontSize = 44;
    public int nameFontSize = 15;
    public int hintFontSize = 16;
    [Tooltip("自动生成的面板随组件销毁时一并删除")]
    public bool destroyGeneratedRootWithComponent = true;

    [System.NonSerialized]
    protected bool m_ownsUiRoot;
    protected Coroutine m_moveRoutine;

    public bool visible
    {
        get
        {
            if (uiRoot == null)
            {
                return false;
            }
            return uiRoot.gameObject.activeSelf;
        }
    }

    protected virtual void OnDestroy()
    {
        if (destroyGeneratedRootWithComponent && m_ownsUiRoot && uiRoot != null)
        {
            Destroy(uiRoot.gameObject);
        }
    }

    /// <summary>显示面板：左右各一个候补成员。</summary>
    public virtual void Show(string leftName, Color leftColor, string rightName, Color rightColor, float duration)
    {
        EnsureBuilt();
        if (uiRoot == null)
        {
            return;
        }

        if (leftNameText != null)
        {
            leftNameText.text = leftName;
        }
        if (rightNameText != null)
        {
            rightNameText.text = rightName;
        }
        if (leftCardImage != null)
        {
            leftCardImage.color = WithAlpha(leftColor, 0.92f);
        }
        if (rightCardImage != null)
        {
            rightCardImage.color = WithAlpha(rightColor, 0.92f);
        }
        if (leftAvatarImage != null)
        {
            leftAvatarImage.color = WithAlpha(leftColor, 1f);
        }
        if (rightAvatarImage != null)
        {
            rightAvatarImage.color = WithAlpha(rightColor, 1f);
        }
        // 有立绘时以白底显示，避免被强调色染色
        ApplyPortrait(leftAvatarImage, leftName);
        ApplyPortrait(rightAvatarImage, rightName);

        uiRoot.gameObject.SetActive(true);
        if (panelGroup != null)
        {
            panelGroup.alpha = 1f;
            panelGroup.blocksRaycasts = false;
            panelGroup.interactable = false;
        }
        SetRemaining(duration);
        SlideInFromBottom();
    }

    /// <summary>用立绘替换头像色块，没有对应资源时保留色块。</summary>
    protected static void ApplyPortrait(Image avatar, string displayName)
    {
        if (avatar == null)
        {
            return;
        }
        var portrait = QtePortraitArt.GetFor(displayName);
        if (portrait != null)
        {
            avatar.sprite = portrait;
            avatar.color = Color.white;
            avatar.preserveAspect = true;
        }
        else
        {
            avatar.sprite = null;
        }
    }

    public virtual void SetRemaining(float remaining)
    {
        if (countdownText == null)
        {
            return;
        }
        float seconds = Mathf.Max(0f, remaining);
        int total = Mathf.FloorToInt(seconds);
        int milli = Mathf.FloorToInt((seconds - total) * 100f);
        countdownText.text = "00:" + total.ToString("00") + ":" + milli.ToString("00");
    }

    public virtual void Hide()
    {
        if (uiRoot == null)
        {
            return;
        }
        if (!uiRoot.gameObject.activeSelf)
        {
            return;
        }
        SlideOutToBottom();
    }

    /// <summary>面板从屏幕下方滑入停靠位（unscaled，暂停下仍流畅）。</summary>
    protected virtual void SlideInFromBottom()
    {
        if (uiRoot == null)
        {
            return;
        }
        uiRoot.gameObject.SetActive(true);
        uiRoot.anchoredPosition = new Vector2(0f, -(panelHeight + 80f));
        MoveUiTo(new Vector2(0f, bottomOffset), null);
    }

    protected virtual void SlideOutToBottom()
    {
        if (uiRoot == null)
        {
            return;
        }
        MoveUiTo(new Vector2(0f, -(panelHeight + 80f)), () =>
        {
            if (uiRoot != null)
            {
                uiRoot.gameObject.SetActive(false);
            }
        });
    }

    protected virtual void MoveUiTo(Vector2 target, System.Action onDone)
    {
        if (m_moveRoutine != null)
        {
            StopCoroutine(m_moveRoutine);
            m_moveRoutine = null;
        }
        m_moveRoutine = StartCoroutine(MoveUiRoutine(target, onDone));
    }

    protected virtual System.Collections.IEnumerator MoveUiRoutine(Vector2 target, System.Action onDone)
    {
        float speed = Mathf.Max(1f, slideSpeed);
        float timeout = 3f;
        while (timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            if (uiRoot == null)
            {
                yield break;
            }
            Vector2 current = uiRoot.anchoredPosition;
            Vector2 next = Vector2.Lerp(current, target, 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));
            uiRoot.anchoredPosition = next;
            if (Vector2.Distance(next, target) < 0.5f)
            {
                uiRoot.anchoredPosition = target;
                break;
            }
            yield return null;
        }
        m_moveRoutine = null;
        onDone?.Invoke();
    }

    /// <summary>
    /// 供运行时 / Editor 菜单调用：在指定父节点下生成一套默认面板并回填所有引用。
    /// 若 uiRoot 已有值则直接返回（不重复生成）。
    /// </summary>
    public virtual RectTransform BuildDefaultUi(Transform parent)
    {
        if (uiRoot != null)
        {
            return uiRoot;
        }
        if (parent == null)
        {
            Debug.LogWarning("[SwitchTimePanel] 生成默认面板需要父节点。");
            return null;
        }

        m_ownsUiRoot = true;

        // 面板根节点
        uiRoot = BattleUi.AddRect(parent, "SwitchTimePanel",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f),
            new Vector2(panelWidth, panelHeight), new Vector2(0f, bottomOffset));
        panelGroup = uiRoot.gameObject.AddComponent<CanvasGroup>();
        panelGroup.alpha = 1f;
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;

        // 半透明底板
        var bg = BattleUi.AddImage(uiRoot, panelBackdrop);
        bg.transform.SetAsFirstSibling();

        // 左右卡片
        BuildCard(uiRoot, -cardOffsetX, out leftCardImage, out leftAvatarImage, out leftNameText);
        BuildCard(uiRoot, cardOffsetX, out rightCardImage, out rightAvatarImage, out rightNameText);

        // 左右按键提示（卡片外侧）
        float hintX = cardOffsetX + cardWidth * 0.5f + 46f;
        var lHintRect = BattleUi.AddRect(uiRoot, "LHint", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(120f, 26f), new Vector2(-hintX, -62f));
        BattleUi.AddText(lHintRect, "LMB", hintFontSize, hintColor);

        var rHintRect = BattleUi.AddRect(uiRoot, "RHint", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(120f, 26f), new Vector2(hintX, -62f));
        BattleUi.AddText(rHintRect, "RMB", hintFontSize, hintColor);

        // 中央：标题 + 倒计时
        var titleRect = BattleUi.AddRect(uiRoot, "Title", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(440f, 30f), new Vector2(0f, 52f));
        var title = BattleUi.AddText(titleRect, "ASSIST!  PICK WITH LMB / RMB", titleFontSize, titleColor);
        hintText = title;

        var countRect = BattleUi.AddRect(uiRoot, "Countdown", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(320f, 80f), new Vector2(0f, -22f));
        countdownText = BattleUi.AddText(countRect, "00:03:00", countdownFontSize, Color.white);

        // 为字体为空的文本补上默认字体
        AssignFontsIfMissing();

        uiRoot.gameObject.SetActive(false);
        return uiRoot;
    }

    protected virtual void EnsureBuilt()
    {
        if (uiRoot != null)
        {
            return;
        }
        if (!autoBuildRuntimeFallback)
        {
            return;
        }

        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[SwitchTimePanel] 场景中找不到 Canvas，无法自动生成选人面板。");
            return;
        }
        BuildDefaultUi(canvas.transform);
    }

    protected virtual void BuildCard(Transform parent, float x, out Image card, out Image avatar, out Text name)
    {
        var cardRect = BattleUi.AddRect(parent, "Card", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(cardWidth, cardHeight), new Vector2(x, 6f));
        card = cardRect.gameObject.AddComponent<Image>();
        card.raycastTarget = false;
        card.color = cardBackdrop;

        // 圆环框放在头像下层
        var ring = QtePortraitArt.ringSprite;
        if (ring != null)
        {
            float ringSize = avatarSize * 1.18f;
            var ringRect = BattleUi.AddRect(cardRect, "Ring", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(ringSize, ringSize), new Vector2(0f, 26f));
            var ringImage = ringRect.gameObject.AddComponent<Image>();
            ringImage.sprite = ring;
            ringImage.preserveAspect = true;
            ringImage.color = Color.white;
            ringImage.raycastTarget = false;
        }

        // 大头像：加载成员立绘，找不到时显示色块
        var avatarRect = BattleUi.AddRect(cardRect, "Avatar", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(avatarSize, avatarSize), new Vector2(0f, 26f));
        avatar = avatarRect.gameObject.AddComponent<Image>();
        avatar.raycastTarget = false;
        avatar.color = Color.white;
        avatar.preserveAspect = true;

        var nameRect = BattleUi.AddRect(cardRect, "Name", new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0.5f, 0f), new Vector2(cardWidth - 16f, 30f), new Vector2(0f, 8f));
        name = BattleUi.AddText(nameRect, "-", nameFontSize, nameColor);
    }

    protected virtual void AssignFontsIfMissing()
    {
        AssignFontIfMissing(leftNameText);
        AssignFontIfMissing(rightNameText);
        AssignFontIfMissing(countdownText);
        AssignFontIfMissing(hintText);
    }

    protected static void AssignFontIfMissing(Text text)
    {
        if (text != null && text.font == null)
        {
            text.font = BattleUi.GetFont();
        }
    }

    protected static Color WithAlpha(Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, alpha);
    }

#if UNITY_EDITOR
    [ContextMenu("生成默认 QTE UI（当前场景）")]
    public void ContextGenerateInScene()
    {
        if (uiRoot != null)
        {
            bool rebuild = UnityEditor.EditorUtility.DisplayDialog("ZZZ QTE UI",
                "已存在面板根节点，是否删除并重新生成？", "重新生成", "取消");
            if (!rebuild)
            {
                return;
            }
            UnityEditor.Undo.DestroyObjectImmediate(uiRoot.gameObject);
            uiRoot = null;
            panelGroup = null;
            leftCardImage = null;
            rightCardImage = null;
            leftAvatarImage = null;
            rightAvatarImage = null;
            leftNameText = null;
            rightNameText = null;
            countdownText = null;
            hintText = null;
        }

        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            UnityEditor.EditorUtility.DisplayDialog("ZZZ QTE UI",
                "当前场景没有 Canvas，请先打开 SampleScene。", "知道了");
            return;
        }

        var root = BuildDefaultUi(canvas.transform);
        if (root == null)
        {
            return;
        }
        UnityEditor.Undo.RegisterCreatedObjectUndo(root.gameObject, "Generate QTE Panel");
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.Selection.activeTransform = root;
    }
#endif
}
