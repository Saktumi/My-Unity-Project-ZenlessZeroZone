using UnityEngine;

/// <summary>
/// 玩家受击请求：携带伤害与攻击者位置，
/// 用于切换受击状态并选择前方 / 后方受击动画。
/// </summary>
public class PlayerHitRequest
{
    public float damage;
    public Vector3 attackerPosition;

    public PlayerHitRequest(float damage, Vector3 attackerPosition)
    {
        this.damage = damage;
        this.attackerPosition = attackerPosition;
    }
}
