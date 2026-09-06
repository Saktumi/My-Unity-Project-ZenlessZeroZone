using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家输入管理器：缓存输入动作，提供按键状态与相机相对方向。
/// </summary>
public class PlayerInputManager : MonoBehaviour
{
    public InputActionAsset actions;
    protected InputAction m_movement;
    protected InputAction m_look;
    protected InputAction m_evade;
    protected InputAction m_runToggle;
    protected InputAction m_normalAttackToggle;
    protected InputAction m_skillToggle;
    protected InputAction m_finishSkillToggle;
    protected InputAction m_pause;

    public Camera m_camera;
    protected const string k_mouseDeviceName = "Mouse";

    private bool m_isRunning = false;
    public bool IsRunning => m_isRunning;

    protected float m_movementDirectionUnlockTime;

    protected virtual void Awake() => CacheActions();

    protected virtual void Update()
    {
        if (IsRunTogglePressed())
        {
            ToggleRunning();
        }
    }

    protected virtual void Start()
    {
        if (actions != null)
        {
            actions.Enable();
        }
    }

    protected virtual void OnEnable() => actions?.Enable();

    protected virtual void OnDisable() => actions?.Disable();

    protected virtual void CacheActions()
    {
        if (actions == null)
        {
            Debug.LogError("[PlayerInputManager] 未在 Inspector 中指定 Input Action Asset！");
            return;
        }

        m_movement = actions["Movement"];
        m_look = actions["Look"];
        m_evade = actions["Evade"];
        m_runToggle = actions["RunToggle"];
        m_normalAttackToggle = actions["NormalAttackToggle"];
        m_skillToggle = actions["SkillToggle"];
        m_finishSkillToggle = actions["FinishSkillToggle"];
        m_pause = actions["Pause"];
    }

    public virtual bool IsRunTogglePressed()
    {
        if (m_runToggle == null) return false;
        return m_runToggle.WasPressedThisFrame();
    }
    public virtual bool IsEvadePressed()
    {
        if (m_evade == null) return false;
        return m_evade.WasPressedThisFrame();
    }

    public virtual void ToggleRunning()
    {
        m_isRunning = !m_isRunning;
    }

    public virtual void SetRunning(bool isRunning)
    {
        if (m_isRunning != isRunning)
        {
            m_isRunning = isRunning;
        }
    }

    public virtual bool IsNormalAttackPressed()
    {
        if (m_normalAttackToggle == null) return false;
        return m_normalAttackToggle.WasPressedThisFrame();
    }

    public virtual bool IsSkillPressed()
    {
        if (m_skillToggle == null) return false;
        return m_skillToggle.WasPressedThisFrame();
    }

    public virtual bool IsFinishSkillPressed()
    {
        if (m_finishSkillToggle == null) return false;
        return m_finishSkillToggle.WasPressedThisFrame();
    }

    public virtual Vector3 GetLookDirection()
    {
        if (m_look == null)
        {
            return Vector3.zero;
        }

        var value = m_look.ReadValue<Vector2>();

        if (IsLookingWithMouse())
        {
            float mouseSensitivity = 0.15f;
            return new Vector3(value.x * mouseSensitivity, 0, value.y * mouseSensitivity);
        }
        
        return GetAxisWithCrossDeadZone(value);
    }

    public virtual Vector3 GetMovementDirection()
    {
        if (Time.time < m_movementDirectionUnlockTime) return Vector3.zero;
        if (m_movement == null) return Vector3.zero;

        var value = m_movement.ReadValue<Vector2>();
        return GetAxisWithCrossDeadZone(value);
    }

    public virtual bool IsLookingWithMouse()
    {
        if (m_look == null || m_look.activeControl == null)
        {
            return false;
        }
        return m_look.activeControl.device.name.Equals(k_mouseDeviceName);
    }

    public virtual Vector3 GetAxisWithCrossDeadZone(Vector2 axis)
    {
        var deadzone = InputSystem.settings.defaultDeadzoneMin;
        axis.x = Mathf.Abs(axis.x) > deadzone ? RemapToDeadzone(axis.x, deadzone) : 0;
        axis.y = Mathf.Abs(axis.y) > deadzone ? RemapToDeadzone(axis.y, deadzone) : 0;
        return new Vector3(axis.x, 0, axis.y);
    }

    protected float RemapToDeadzone(float value, float deadzone) => 
        (value - (value > 0 ? -deadzone : deadzone)) / (1 - deadzone);

    public virtual Vector3 GetMovementCameraDirection()
    {
        var direction = GetMovementDirection();
        
        if (direction.sqrMagnitude > 0)
        {
            var rotation = Quaternion.AngleAxis(m_camera.transform.eulerAngles.y, Vector3.up);
            direction = rotation * direction;
            direction = direction.normalized;
        }

        return direction;
    }

    public virtual bool GetPauseDown()
    {
        if (m_pause == null) return false;
        return m_pause.WasPressedThisFrame();
    }
}
