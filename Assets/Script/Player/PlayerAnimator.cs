using UnityEngine;

/// <summary>
/// 玩家动画器：把输入映射为 Animator 参数，
/// 并在 OnAnimatorMove 中将根运动位移应用到 CharacterController。
/// </summary>
[RequireComponent(typeof(Player))]
[AddComponentMenu("Player/Player Animator")]

public class PlayerAnimator : MonoBehaviour
{
    [Header("Speed 映射")]
    [SerializeField] private float walkSpeedMultiplier = 0.5f;
    [SerializeField] private float runSpeedMultiplier = 2f;
    [Tooltip("Speed 参数平滑时间")]
    [SerializeField] private float speedSmoothTime = 0.15f;
    public Animator animator;  
    protected Player player;

    public bool isRun;

    private static readonly int s_speedHash = Animator.StringToHash("Speed");
    private static readonly int s_isRunHash = Animator.StringToHash("IsRun");
    private static readonly int s_isMovingHash = Animator.StringToHash("isMoving");

    private float m_currentSpeed;
    private float m_speedVelocity;

    protected virtual void Update()
    {
        if (player == null)
        {
            player = GetComponent<Player>();
        }

        if (animator == null || player == null)
        {
            return;
        }

        var currentStats = player.stats != null ? player.stats.current : null;
        var topSpeed = currentStats != null ? Mathf.Max(currentStats.topSpeed, 0.01f) : 1f;

        isRun = player.inputs != null && player.inputs.IsRunning;
        bool hasInput = player.inputs != null && player.inputs.GetMovementDirection().sqrMagnitude > 0.01f;

        // 根运动速度驱动 Speed 参数，无输入时衰减到 0
        float rootNormalized = Mathf.Clamp01(animator.velocity.magnitude / topSpeed);
        float speedMultiplier = isRun ? runSpeedMultiplier : walkSpeedMultiplier;
        float targetSpeed = hasInput ? Mathf.Clamp01(rootNormalized * speedMultiplier) : 0f;

        animator.SetBool(s_isRunHash, isRun);
        animator.SetBool(s_isMovingHash, hasInput);

        m_currentSpeed = Mathf.SmoothDamp(m_currentSpeed, targetSpeed, ref m_speedVelocity, speedSmoothTime);
        m_currentSpeed = Mathf.Clamp01(m_currentSpeed);

        animator.SetFloat(s_speedHash, m_currentSpeed);
    }

    void OnAnimatorMove()
    {
        if (player == null)
        {
            player = GetComponent<Player>();
        }

        // 部分攻击段临时禁用根运动，避免角色被带离目标
        if (player != null && player.suppressRootMotion)
        {
            return;
        }

        Vector3 deltaPosition = animator.deltaPosition;
        if (player.controller != null && player.controller.enabled)
        {
            player.controller.Move(deltaPosition);
        }
        else
        {
            transform.position += deltaPosition;
        }
    }

}
