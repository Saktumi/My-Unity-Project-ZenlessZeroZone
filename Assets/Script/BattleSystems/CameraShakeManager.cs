using UnityEngine;

/// <summary>
/// 相机震屏器：渲染前叠加随机位移 / 旋转，不依赖 timeScale，
/// 顿帧与慢动作期间震屏依然可见。
/// </summary>
public class CameraShakeManager : MonoBehaviour
{
    public static CameraShakeManager instance { get; protected set; }

    [Header("强度映射")]
    [Tooltip("force=1 时的位移幅度（世界单位）")]
    public float positionAmplitude = 0.035f;
    [Tooltip("force=1 时的旋转幅度（度）")]
    public float rotationAmplitude = 1.1f;
    [Tooltip("衰减曲线指数：越大衰减越快")]
    public float decayPower = 1.6f;

    protected Camera m_cam;
    protected Transform m_t;
    protected float m_strength;
    protected float m_duration;
    protected float m_elapsed;
    protected float m_frequency = 26f;
    protected float m_seedX;
    protected float m_seedY;
    protected float m_seedZ;
    protected bool m_shaking;

    public bool IsShaking => m_shaking;

    public static CameraShakeManager Ensure()
    {
        if (instance != null)
        {
            return instance;
        }
        var cam = Camera.main;
        if (cam == null)
        {
            return null;
        }
        var existing = cam.GetComponent<CameraShakeManager>();
        if (existing == null)
        {
            existing = cam.gameObject.AddComponent<CameraShakeManager>();
        }
        return existing;
    }

    /// <summary>把震屏器挂到指定相机。</summary>
    public static CameraShakeManager AttachTo(Camera cam)
    {
        if (cam == null)
        {
            return null;
        }
        var manager = cam.GetComponent<CameraShakeManager>();
        if (manager == null)
        {
            manager = cam.gameObject.AddComponent<CameraShakeManager>();
        }
        return manager;
    }

    protected virtual void Awake()
    {
        m_cam = GetComponent<Camera>();
        m_t = transform;

        // 只有主相机注册全局单例，其它机位可独立挂载
        bool isMain = m_cam != null && m_cam.CompareTag("MainCamera");
        if (isMain && instance != null && instance != this)
        {
            Destroy(this);
            return;
        }
        if (isMain)
        {
            instance = this;
        }
        if (m_cam != null)
        {
            Camera.onPreCull += OnCameraPreCull;
        }
    }

    protected virtual void OnDestroy()
    {
        if (m_cam != null)
        {
            Camera.onPreCull -= OnCameraPreCull;
        }
        bool isMain = m_cam != null && m_cam.CompareTag("MainCamera");
        if (isMain && instance == this)
        {
            instance = null;
        }
    }

    /// <summary>发起一次震屏，force = 1 为正常强度。</summary>
    public virtual void Shake(float force, float duration = 0.22f, float frequency = 26f)
    {
        if (force <= 0f || duration <= 0f)
        {
            return;
        }
        m_strength = Mathf.Max(m_strength, force);
        m_duration = Mathf.Max(0.02f, duration);
        m_frequency = Mathf.Max(1f, frequency);
        m_elapsed = 0f;
        m_shaking = true;
        m_seedX = Random.Range(-1000f, 1000f);
        m_seedY = Random.Range(-1000f, 1000f);
        m_seedZ = Random.Range(-1000f, 1000f);
    }

    protected virtual void Update()
    {
        if (!m_shaking)
        {
            return;
        }
        m_elapsed += Time.unscaledDeltaTime;
        if (m_elapsed >= m_duration)
        {
            m_shaking = false;
            m_strength = 0f;
        }
    }

    protected virtual void OnCameraPreCull(Camera cam)
    {
        if (cam != m_cam || !m_shaking || m_t == null)
        {
            return;
        }

        float t = Mathf.Clamp01(m_elapsed / m_duration);
        float decay = Mathf.Pow(1f - t, decayPower);
        float amount = m_strength * decay;
        float phase = m_elapsed * m_frequency;

        Vector3 pos = new Vector3(
            Wave(m_seedX, phase) * positionAmplitude,
            Wave(m_seedY, phase) * positionAmplitude,
            Wave(m_seedZ, phase) * positionAmplitude) * amount;

        Vector3 rot = new Vector3(
            Wave(m_seedZ, phase * 1.1f) * rotationAmplitude,
            Wave(m_seedX, phase * 0.9f) * rotationAmplitude,
            Wave(m_seedY, phase * 1.3f) * rotationAmplitude) * amount * 0.5f;

        m_t.position += pos;
        m_t.rotation *= Quaternion.Euler(rot);
    }

    /// <summary>双正弦叠加制造不规则抖动。</summary>
    protected static float Wave(float seed, float phase)
    {
        return Mathf.Sin(seed * 0.1f + phase * Mathf.PI * 2f)
            + Mathf.Sin(seed + phase * Mathf.PI * 1.3f) * 0.55f;
    }
}
