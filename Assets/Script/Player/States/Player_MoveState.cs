using UnityEngine;

/// <summary>
/// 玩家移动状态：按输入方向朝相机相对方向移动，并平滑转向。
/// </summary>
public class Player_MoveState : PlayerState
{
    protected override void OnEnter(Player player)
    {
        if (player.m_animator == null)
        {
            return;
        }

        bool isRun = player.p_animator != null && player.p_animator.isRun;
        if (!isRun)
        {
            player.m_animator.CrossFadeInFixedTime("开始行走", 0.15f);
        }
        else
        {
            player.m_animator.CrossFadeInFixedTime("开始跑步", 0.15f);
        }
    }

    protected override void OnExit(Player player)
    {
    }

    protected override void OnStep(Player player)
    {
        Vector3 inputDirection = player.inputs.GetMovementCameraDirection();

        if (inputDirection.sqrMagnitude > 0)
        {
            player.FaceDirectionSmooth(inputDirection);
        }
        else
        {
            player.states.Change<Player_IdleState>();
        }
    }

    protected override void OnContact(Player player, Collider other)
    {

    }
}
