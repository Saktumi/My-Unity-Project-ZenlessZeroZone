
using UnityEngine;

/// <summary>玩家属性配置：移动、连击、技能与能量的参数资产。</summary>
[CreateAssetMenu(fileName = "DefaultPlayerStats", menuName = "Custom/Player Stats")]
public class PlayerStats : EntityStats<PlayerStats>
{
    [Header("General Stats")]
    public float rotationSpeed = 970f;

    [Header("Motion Stats")]
    public float topSpeed = 6f;

    [Header("Idle Settings")]
    public float idleToAFKTime = 5f;
    public float afkToIdleTime = 3f;

    [Header("Evade Settings")]
    public float evadeChainWindow = 1f;
    public float evadeCooldown = 2f;

    [Header("Normal Attack")]
    public string[] normalName;
    public string rushAttackName;

    [Header("Skill Settings")]
    public string skillName;
    public string specialSkillName;
    public string finishSkillName;
    public string finishSkillPassName;

    [Header("Assist Skill / QTE（连携技）")]
    [Tooltip("破防 QTE 选择成员后播放的连携技动画")]
    public string assistSkillName = "Qte";
    [Tooltip("连携技伤害 = SkillAttack × 该倍率")]
    public float assistSkillMultiple = 1.6f;


    [Header("Attack Snap（攻击吸附）")]
    [Tooltip("攻击起手吸附半径")]
    public float attackSnapRange = 10f;
    [Tooltip("目标朝向夹角超过该角度则不吸附，180 表示不限")]
    public float attackSnapMaxAngle = 180f;
    [Tooltip("吸附时向目标方向推进的距离，0 表示只转向")]
    public float attackSnapPullDistance = 0.8f;
    [Tooltip("打印吸附调试日志")]
    public bool attackSnapDebug = false;

    [Header("Attack Hit（命中检测 / 震屏）")]
    [Tooltip("命中窗口起始比例")]
    public float hitWindowStart = 0.25f;
    [Tooltip("命中窗口结束比例")]
    public float hitWindowEnd = 0.7f;
    [Tooltip("命中判定范围")]
    public float hitRange = 2.5f;
    [Tooltip("命中判定夹角（度）")]
    public float hitAngle = 60f;
    [Tooltip("命中震屏力度，1 为正常")]
    public float hitShakeForce = 1f;
    [Tooltip("命中顿帧时长（秒），0 为关闭")]
    public float hitStopTime = 0.06f;

    [Header("Finish Skill Cinematic（大招演出）")]
    [Tooltip("大招起手闪现到目标身边的水平距离，0 使用默认值")]
    public float finishSkillSnapDistance = 1.6f;
    [Tooltip("大招命中结算时机（动画播放比例）")]
    public float finishSkillHitTime = 0.32f;
    [Tooltip("终结技伤害保底比例（按敌人最大生命）")]
    public float finishSkillDamageRatio = 0.2f;
    [Tooltip("大招演出期间是否隔离场景")]
    public bool finishSkillIsolateScene = true;

    [Header("Hit Reaction Animations（受击动画）")]
    [Tooltip("击飞动画：受击来自前方时播放")]
    public string hitFlyFrontName = "HitFly_Front";
    [Tooltip("击飞动画：受击来自后方时播放")]
    public string hitFlyBackName = "HitFly_Back";

    [Header("Hit Reaction Settings（受击规则）")]
    [Tooltip("受击动画播放到该比例后允许按闪避取消硬直")]
    public float hitEvadeCancelTime = 0.7f;

    [Header("Attribute")]
    public float Hp = 100f;
    public float Attack = 20;
    public float SkillAttack = 50;
    public float SpecialSkillMultiple = 1.5f;
    public float FinishSkillMultiple = 2f;
    public float maxSpecialSkillEnergy = 100f;
    public float specialSkillEnergyCost = 20f;

    public float criticalPer =0.5f;
    public float criticalMultiple = 1.5f;
    public float AttackTanFanPower = 5f;
    public float TanFanPower = 20f;
}
