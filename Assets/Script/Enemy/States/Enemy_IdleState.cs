using UnityEngine;

public class Enemy_IdleState : EnemyState
{
    private bool m_isAFK = false;
    private float m_timer = 0f;
    private Animator m_animator;

    private static readonly int s_isAFKHash = Animator.StringToHash("IsAFK");

    protected override void OnEnter(Enemy enemy)
    {
        m_isAFK = false;
        m_timer = 0f;

        m_animator = enemy.GetComponent<Animator>();
        if (m_animator == null)
        {
            m_animator = enemy.GetComponentInChildren<Animator>();
        }

        if (m_animator != null)
        {
            m_animator.SetBool(s_isAFKHash, false);
        }
    }

    protected override void OnStep(Enemy enemy)
    {
        var stats = enemy.stats != null ? enemy.stats.current : null;
        if (stats == null) return;

        if (m_animator == null) return;

        // 攻击间隔期间面向目标
        if (enemy.HasTarget)
        {
            enemy.FaceTargetSmooth();
        }

        var direction = enemy.GetMovementDirection();
        if (direction.sqrMagnitude > 0.01f)
        {
            m_animator.SetBool(s_isAFKHash, false);
            enemy.states.Change<Enemy_MoveState>();
            return;
        }

        m_timer += Time.deltaTime;

        if (!m_isAFK)
        {
            if (m_timer >= stats.idleToAFKTime)
            {
                m_isAFK = true;
                m_timer = 0f;
                m_animator.SetBool(s_isAFKHash, true);
            }
        }
        else
        {
            if (m_timer >= stats.afkToIdleTime)
            {
                m_isAFK = false;
                m_timer = 0f;
                m_animator.SetBool(s_isAFKHash, false);
            }
        }
    }

    protected override void OnExit(Enemy enemy)
    {
        m_isAFK = false;
        m_timer = 0f;

        if (m_animator != null)
        {
            m_animator.SetBool(s_isAFKHash, false);
        }
    }

    protected override void OnContact(Enemy enemy, Collider other) { }
}
