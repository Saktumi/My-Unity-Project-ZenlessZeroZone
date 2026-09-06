using UnityEngine;

[CreateAssetMenu(fileName = "DefaultEnemyStats", menuName = "Custom/Enemy Stats")]
public class EnemyStats : EntityStats<EnemyStats>
{
    [Header("General Stats")]
    public float rotationSpeed = 720f;

    [Header("Motion Stats")]
    public float topSpeed = 4f;

    [Header("Move Animations")]
    public string walkAnimName = "开始行走";
    public string runAnimName = "开始跑步";

    [Header("Idle Settings")]
    public float idleToAFKTime = 5f;
    public float afkToIdleTime = 3f;

    [Header("Combat Settings")]
    public float detectRadius = 12f;
    public float attackRange = 2.5f;
    public float attackCooldownMin = 1f;
    public float attackCooldownMax = 3f;

    [Header("Normal Attack")]
    [Tooltip("攻击动画名池，进入攻击时随机选取")]
    public string[] normalName =
    {
        "Goblin_Ani_Attack_01",
        "Goblin_Ani_Attack_02",
        "Goblin_Ani_Attack_03",
        "Goblin_Ani_Attack_04",
    };

    [Header("Attack Hit（攻击命中判定）")]
    [Tooltip("攻击命中窗口起始比例")]
    public float attackHitWindowStart = 0.25f;
    [Tooltip("攻击动画命中窗口结束比例")]
    public float attackHitWindowEnd = 0.8f;
    [Tooltip("命中判定距离 = attackRange × 该倍率")]
    public float hitRangeScale = 1.4f;
    [Tooltip("命中判定夹角（度）")]
    public float hitAngle = 75f;

    [Header("Attack Damage（攻击伤害）")]
    [Tooltip("伤害为角色最大生命的随机下限比例")]
    public float attackDamageMinRatio = 0.1f;
    [Tooltip("伤害为角色最大生命的随机上限比例")]
    public float attackDamageMaxRatio = 0.5f;

    [Header("Reaction Animations")]
    public string hitAnimName;
    public string stunAnimName;
    public string deathAnimName;

    [Header("Daze / Break（失衡破防）")]
    [Tooltip("失衡值上限，打满后进入破防眩晕")]
    public float maxDaze = 80f;
    [Tooltip("受到伤害转化为失衡值的比例")]
    public float dazeGainRatio = 1.5f;
    [Tooltip("停止受击后开始回复失衡值的延迟")]
    public float dazeDecayDelay = 3f;
    [Tooltip("失衡值每秒回复量")]
    public float dazeDecayPerSecond = 10f;
    [Tooltip("破防眩晕（QTE 窗口）时长")]
    public float stunHoldTime = 4f;

    [Header("Attribute")]
    public float Hp = 1000000f;
    public float Attack = 0.2f;
    public float TanFan = 100f;
}
