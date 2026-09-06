using UnityEngine;

[RequireComponent(typeof(Enemy))]
[AddComponentMenu("Enemy/Enemy Animator")]
public class EnemyAnimator : MonoBehaviour
{
    [Header("Speed 映射")]
    [SerializeField] private float walkSpeedMultiplier = 0.5f;
    [SerializeField] private float runSpeedMultiplier = 2f;
    [Tooltip("Speed 参数平滑时间")]
    [SerializeField] private float speedSmoothTime = 0.15f;
    public Animator animator;
    protected Enemy enemy;

    public bool isRun;

    private static readonly int s_speedHash = Animator.StringToHash("Speed");
    private static readonly int s_isRunHash = Animator.StringToHash("IsRun");
    private static readonly int s_isMovingHash = Animator.StringToHash("isMoving");

    private float m_currentSpeed;
    private float m_speedVelocity;

    protected virtual void Update()
    {
        if (enemy == null)
        {
            enemy = GetComponent<Enemy>();
        }

        if (animator == null || enemy == null)
        {
            return;
        }

        // 死亡后停止写入移动参数，避免复活 / 继续行走等异常
        if (enemy.deathLocked)
        {
            return;
        }

        var currentStats = enemy.stats != null ? enemy.stats.current : null;
        var topSpeed = currentStats != null ? Mathf.Max(currentStats.topSpeed, 0.01f) : 1f;

        bool hasInput = enemy.GetMovementDirection().sqrMagnitude > 0.01f;

        // 追击中超出攻击范围时跑步，否则走路
        isRun = hasInput && enemy.HasTarget
            && enemy.DistanceToTarget > (currentStats != null ? currentStats.attackRange : 2f);

        // 根运动速度驱动 Speed 参数，无移动时衰减到 0
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
        if (enemy == null)
        {
            enemy = GetComponent<Enemy>();
        }

        Vector3 deltaPosition = animator.deltaPosition;
        if (enemy.controller != null && enemy.controller.enabled)
        {
            enemy.controller.Move(deltaPosition);
        }
        else
        {
            transform.position += deltaPosition;
        }
    }
}
