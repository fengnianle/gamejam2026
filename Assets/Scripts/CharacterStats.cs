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
    
    [Header("显示信息")]
    [Tooltip("角色名称")]
    public string characterName = "Character";
}
