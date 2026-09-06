using UnityEngine;

public class Player_IdleState : PlayerState
{
    private bool m_isAFK;
    private float m_timer;
    private Animator m_animator;

    private static readonly int s_isAFKHash = Animator.StringToHash("IsAFK");

    protected override void OnEnter(Player player)
    {
        m_isAFK = false;
        m_timer = 0f;

        m_animator = player.GetComponent<Animator>();
        if (m_animator == null)
        {
            m_animator = player.GetComponentInChildren<Animator>();
        }

        if (m_animator != null)
        {
            m_animator.SetBool(s_isAFKHash, false);
        }
    }

    protected override void OnStep(Player player)
    {
        var stats = player.stats != null ? player.stats.current : null;
        if (stats == null || m_animator == null)
        {
            return;
        }

        var inputDirection = player.inputs.GetMovementDirection();
        if (inputDirection.sqrMagnitude > 0.01f)
        {
            m_animator.SetBool(s_isAFKHash, false);
            player.states.Change<Player_MoveState>();
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
        else if (m_timer >= stats.afkToIdleTime)
        {
            m_isAFK = false;
            m_timer = 0f;
            m_animator.SetBool(s_isAFKHash, false);
        }
    }

    protected override void OnExit(Player player)
    {
        m_isAFK = false;
        m_timer = 0f;

        if (m_animator != null)
        {
            m_animator.SetBool(s_isAFKHash, false);
        }
    }

    protected override void OnContact(Player player, Collider other) { }
}
