using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>第三人称轨道相机：旋转、缩放与向后移动后撤补偿。</summary>
[RequireComponent(typeof(CinemachineCamera))]
[AddComponentMenu("Player/PlayerCamera")]
public class PlayerCamera : MonoBehaviour
{
    [Header("参照")]
    public Player player;
    public Transform sizeReference;   // 角色大小参照物（场景中的 Cylinder），留空自动查找

    public Transform follow;
    public Transform LookAt;

    [Header("灵敏度")]
    public float mouseXSensitivity = 200f;
    public float mouseYSensitivity = 90f;

    [Header("距离（按角色高度倍率）")]
    public float distanceScale = 2.2f;   // 默认距离 = 角色高度 × 该值
    public float minDistanceScale = 1.5f;
    public float maxDistanceScale = 4f;
    public float zoomSpeed = 2f;

    [Header("轨道高度（按角色高度倍率）")]
    public float topHeightScale = 0.55f;     // 俯视拉满时的相机高度
    public float centerHeightScale = 0.3f;   // 默认高度
    public float bottomHeightScale = 0.1f;   // 仰视拉满时的相机高度

    [Header("俯仰")]
    public float verticalMin = -15f;
    public float verticalMax = 50f;
    public float initialAngle = 20f;

    [Header("平滑（越大越跟手）")]
    public float smoothSpeed = 10f;

    [Header("向后移动补偿")]
    public float backwardPushDistance = 1f;   // 向后移动时镜头额外后退的距离（米）
    public float radiusLerpSpeed = 6f;        // 镜头后退/回位时的平滑速度

    protected CinemachineCamera m_camera;
    protected CinemachineOrbitalFollow m_orbit;
    protected CinemachineRotationComposer m_aim;

    protected InputAction m_lookAction;
    protected InputAction m_scrollAction;

    protected float m_characterHeight = 1f;
    protected Vector2 m_smoothLook;
    protected float m_baseRadius;
    protected float m_currentRadius;
    protected float m_radiusScale = 1f;

    protected const string k_ReferenceName = "Cylinder";

    protected virtual void Awake()
    {
        m_camera = GetComponent<CinemachineCamera>();
        m_orbit = GetComponent<CinemachineOrbitalFollow>();
        m_aim = GetComponent<CinemachineRotationComposer>();
        if (player == null)
        {
            player = Object.FindFirstObjectByType<Player>();
        }
    }

    protected virtual void Start()
    {
        if (player == null || m_camera == null || m_orbit == null || m_aim == null)
        {
            Debug.LogError("PlayerCamera：缺少 CinemachineCamera / CinemachineOrbitalFollow / CinemachineRotationComposer 组件或 Player 引用");
            return;
        }

        if (player.inputs != null && player.inputs.actions != null)
        {
            m_lookAction = player.inputs.actions["Look"];
            m_scrollAction = player.inputs.actions["ScrollWheel"];
        }
        if (m_lookAction == null || m_scrollAction == null)
        {
            Debug.LogError("PlayerCamera：输入资产中缺少 Look / ScrollWheel 动作");
            return;
        }

        m_camera.Follow = follow;
        m_camera.LookAt = LookAt;
        m_aim.TargetOffset = new Vector3(0f, m_characterHeight * 0.45f, 0f);

        // 三环轨道：距离与高度按角色高度缩放
        m_orbit.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.ThreeRing;
        ApplyOrbitRadius(m_characterHeight * distanceScale);

        // 关闭自动回中并限制俯仰范围
        m_orbit.VerticalAxis.Range = new Vector2(verticalMin, verticalMax);
        m_orbit.HorizontalAxis.Recentering.Enabled = false;
        m_orbit.VerticalAxis.Recentering.Enabled = false;

        // 减小阻尼，避免跑步时镜头滞后
        m_orbit.TrackerSettings.PositionDamping = new Vector3(0.2f, 0.2f, 0.2f);
        m_orbit.TrackerSettings.RotationDamping = new Vector3(0.1f, 0.1f, 0.1f);

        // 初始机位：角色正后方，略带俯视
        m_baseRadius = m_characterHeight * distanceScale;
        m_currentRadius = m_baseRadius;
        m_orbit.HorizontalAxis.Value = player.transform.rotation.eulerAngles.y;
        m_orbit.VerticalAxis.Value = initialAngle;
    }

    protected virtual void Update()
    {
        if (m_orbit == null || m_lookAction == null)
        {
            return;
        }

        var look = m_lookAction.ReadValue<Vector2>();
        m_smoothLook = Vector2.Lerp(m_smoothLook, look, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));

        m_orbit.HorizontalAxis.Value += m_smoothLook.x * mouseXSensitivity * Time.deltaTime;
        m_orbit.VerticalAxis.Value = Mathf.Clamp(
            m_orbit.VerticalAxis.Value - m_smoothLook.y * mouseYSensitivity * Time.deltaTime,
            verticalMin, verticalMax);

        var scroll = m_scrollAction.ReadValue<Vector2>();
        if (Mathf.Abs(scroll.y) > 0.001f)
        {
            m_baseRadius = Mathf.Clamp(m_baseRadius - scroll.y * zoomSpeed,
                m_characterHeight * minDistanceScale, m_characterHeight * maxDistanceScale);
        }

        float backAmount = 0f;
        if (player != null && player.inputs != null)
        {
            var move = player.inputs.GetMovementDirection();
            backAmount = Mathf.Clamp01(-move.z);   // 输入 z < 0 = 相对相机向后移动
        }
        float targetRadius = m_baseRadius * m_radiusScale + backwardPushDistance * backAmount;

        m_currentRadius = Mathf.Lerp(m_currentRadius, targetRadius,
            1f - Mathf.Exp(-radiusLerpSpeed * Time.deltaTime));
        ApplyOrbitRadius(m_currentRadius);
    }

    /// <summary>临时缩放镜头距离，演出结束后复位为 1。</summary>
    public virtual void SetCameraZoomScale(float scale)
    {
        m_radiusScale = Mathf.Clamp(scale, 0.3f, 2f);
    }

    public virtual void ResetCameraZoomScale()
    {
        m_radiusScale = 1f;
    }

    /// <summary>切人时更新跟随与注视目标，保留当前视角。</summary>
    public virtual void Retarget(Player newPlayer, Transform followTarget, Transform lookTarget)
    {
        player = newPlayer;
        if (followTarget != null)
        {
            follow = followTarget;
        }
        if (lookTarget != null)
        {
            LookAt = lookTarget;
        }
        if (follow == null && player != null)
        {
            follow = player.transform;
        }
        if (LookAt == null)
        {
            LookAt = follow;
        }

        if (m_camera != null)
        {
            m_camera.Follow = follow;
            m_camera.LookAt = LookAt;
        }
    }

    // 同步三个轨道环的半径与高度
    protected virtual void ApplyOrbitRadius(float radius)
    {
        m_orbit.Orbits.Top = new Cinemachine3OrbitRig.Orbit
        {
            Radius = radius,
            Height = m_characterHeight * topHeightScale,
        };
        m_orbit.Orbits.Center = new Cinemachine3OrbitRig.Orbit
        {
            Radius = radius,
            Height = m_characterHeight * centerHeightScale,
        };
        m_orbit.Orbits.Bottom = new Cinemachine3OrbitRig.Orbit
        {
            Radius = radius,
            Height = m_characterHeight * bottomHeightScale,
        };
        m_orbit.Orbits.SplineCurvature = 0.5f;
    }
}
