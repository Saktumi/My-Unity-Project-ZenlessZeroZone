using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

/// <summary>面板淡入淡出与缩放动画，暂停期间仍可播放。</summary>
public class UIAnimator : MonoBehaviour
{
    public UnityEvent OnShow;
    public UnityEvent OnHide;

    public bool hidenOnAwake = true;
    public float duration = 0.3f;
    public Ease showEase = Ease.OutBack;
    public Ease hideEase = Ease.InBack;

    [Range(0f, 1f)]
    public float startAlpha = 0f;
    public Vector2 startScale = new Vector2(0.8f, 0.8f);

    protected CanvasGroup m_canvasGroup;
    protected RectTransform m_rectTransform;
    protected Tween m_tween;

    protected virtual void Awake()
    {
        m_canvasGroup = GetComponent<CanvasGroup>();
        if (m_canvasGroup == null)
        {
            m_canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        m_rectTransform = GetComponent<RectTransform>();

        if (hidenOnAwake)
        {
            gameObject.SetActive(false);
        }
    }

    // SetUpdate(true)：暂停（timeScale = 0）时动画照常播放
    public virtual void Show()
    {
        KillTween();
        gameObject.SetActive(true);

        m_canvasGroup.alpha = startAlpha;
        if (m_rectTransform != null)
        {
            m_rectTransform.localScale = startScale;
        }

        var sequence = DOTween.Sequence();
        sequence.Append(m_canvasGroup.DOFade(1f, duration).SetEase(showEase));
        if (m_rectTransform != null)
        {
            sequence.Join(m_rectTransform.DOScale(Vector3.one, duration).SetEase(showEase));
        }
        sequence.SetUpdate(true);
        m_tween = sequence;

        OnShow?.Invoke();
    }

    // 播完动画再隐藏物体
    public virtual void Hide()
    {
        KillTween();
        if (m_rectTransform != null)
        {
            m_rectTransform.localScale = Vector3.one;
        }

        var sequence = DOTween.Sequence();
        sequence.Append(m_canvasGroup.DOFade(0f, duration).SetEase(hideEase));
        if (m_rectTransform != null)
        {
            sequence.Join(m_rectTransform.DOScale(startScale, duration).SetEase(hideEase));
        }
        sequence.SetUpdate(true);
        sequence.OnComplete(() => gameObject.SetActive(false));
        m_tween = sequence;

        OnHide?.Invoke();
    }

    public virtual void SetActive(bool value) => gameObject.SetActive(value);

    protected virtual void KillTween()
    {
        if (m_tween != null)
        {
            m_tween.Kill();
            m_tween = null;
        }
    }
}
