using UnityEngine;

public class Enemy_DeathState : EnemyState
{
    private const float k_CrossFadeTime = 0.1f;

    private string m_deathAnimName;

    protected override void OnEnter(Enemy enemy)
    {
        // 清空残留请求，避免死后被旧请求驱动
        enemy.ClearCombatRequests();

        var stats = enemy.stats != null ? enemy.stats.current : null;
        m_deathAnimName = stats != null ? stats.deathAnimName : null;

        // 禁用控制器，让根运动直接作用于 Transform
        if (enemy.controller != null)
        {
            enemy.controller.enabled = false;
        }

        if (!string.IsNullOrEmpty(m_deathAnimName) && enemy.m_animator != null)
        {
            enemy.m_animator.CrossFadeInFixedTime(m_deathAnimName, k_CrossFadeTime);
        }
    }

    protected override void OnExit(Enemy enemy)
    {
        m_deathAnimName = null;
    }

    protected override void OnStep(Enemy enemy)
    {
    }

    protected override void OnContact(Enemy enemy, Collider other) { }
}
