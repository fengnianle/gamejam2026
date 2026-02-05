using UnityEngine;

/// <summary>
/// 角色属性配置（ScriptableObject）
/// 用于在Inspector中配置角色的初始属性，与运行时状态分离
/// </summary>
[CreateAssetMenu(fileName = "New Character Stats", menuName = "Game/Character Stats", order = 1)]
public class CharacterStats : ScriptableObject
{
    [Header("生命值配置")]
    [Tooltip("最大生命值")]
    public float maxHealth = 100f;
    
    [Header("攻击配置")]
    [Tooltip("攻击力")]
    public float attackDamage = 50f;
    
    [Tooltip("同步攻击伤害（双方出相同攻击时的伤害值）")]
    public float clashDamage = 40f;
    
    [Tooltip("攻击静止目标伤害（玩家攻击未出招的Boss时的伤害值，鼓励玩家进行反制而非乱打）")]
    public float idleAttackDamage = 10f;
    
    [Header("显示信息")]
    [Tooltip("角色名称")]
    public string characterName = "Character";
}

// 玩家配置建议：
// Max Health: 100
// Attack Damage: 20      (反制成功时对Boss造成的伤害，27次杀Boss)
// Clash Damage: 10       (拼刀时给Boss造成的伤害)
// Idle Attack Damage: 5  (偷刀伤害)

// Boss配置建议：
// Max Health: 540        (540 / 20 = 27次反制)
// Attack Damage: 34      (击中玩家时造成的伤害，34 * 3 = 102 > 100，3刀带走玩家)
// Clash Damage: 15       (拼刀时给玩家造成的伤害，玩家能抗 100 / 15 = 6.6 次拼刀)
// Idle Attack Damage: 0  (Boss通常不会偷玩家的刀，设为0或保持很少)