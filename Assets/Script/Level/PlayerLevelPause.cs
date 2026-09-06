using UnityEngine;

/// <summary>监听 Pause 输入并切换全局暂停状态。</summary>
public class PlayerLevelPause : MonoBehaviour
{
    protected Player m_player;
    protected LevelPauser m_pauser;

    protected virtual void Start()
    {
        m_player = GetComponent<Player>();
        m_pauser = LevelPauser.instance;
    }

    protected virtual void Update()
    {
        if (m_player != null && m_player.inputs != null && m_player.inputs.GetPauseDown())
        {
            if (m_pauser != null)
            {
                var value = m_pauser.paused;
                m_pauser.Pause(!value);
            }
        }
    }
}
