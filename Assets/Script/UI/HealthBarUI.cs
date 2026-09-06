using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>运行时血条刷新：按血量比例调整填充条宽度。</summary>
public class HealthBarUI : MonoBehaviour
{
    private Image m_fillImage;
    private float m_baseWidth;
    private Func<float> m_currentGetter;
    private Func<float> m_maxGetter;
    private float m_lastLoggedAmount = -1f;

    /// <summary>绑定填充图、满血宽度与血量取值委托。</summary>
    public void Setup(Image fillImage, float baseWidth, Func<float> currentGetter, Func<float> maxGetter)
    {
        m_fillImage = fillImage;
        m_baseWidth = baseWidth;
        m_currentGetter = currentGetter;
        m_maxGetter = maxGetter;
    }

    private void Update()
    {
        if (m_fillImage == null || m_currentGetter == null || m_maxGetter == null)
        {
            return;
        }

        float max = Mathf.Max(0f, m_maxGetter());
        float current = Mathf.Max(0f, m_currentGetter());
        float amount = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        var rect = (RectTransform)m_fillImage.transform;
        rect.sizeDelta = new Vector2(m_baseWidth * amount, rect.sizeDelta.y);

        if (Mathf.Abs(amount - m_lastLoggedAmount) > 0.005f)
        {
            GameLog.Log($"[HealthBarUI:{gameObject.name}] 血量比例 {(amount * 100f):0.#}%");
            m_lastLoggedAmount = amount;
        }
    }
}
