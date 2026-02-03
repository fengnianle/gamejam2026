using UnityEngine;

/// <summary>
/// 攻击关系判定工具类
/// 实现类似"剪刀石头布"的循环压制机制：
/// AttackX（石头） -> AttackB（剪刀） -> AttackY（布） -> AttackX（石头）
/// 即：X压制B，B压制Y，Y压制X
/// </summary>
public static class AttackRelationship
{
    /// <summary>
    /// 攻击结果枚举
    /// </summary>
    public enum AttackResult
    {
        Counter,    // 压制成功（玩家出对应压制招式）
        Clash,      // 同时攻击（双方出相同招式）
        Hit         // 被击中（玩家出被压制的招式或不出招）
    }

    /// <summary>
    /// 判定攻击结果
    /// </summary>
    /// <param name="enemyAttack">敌人的攻击类型</param>
    /// <param name="playerAttack">玩家的攻击类型（可为null表示不出招）</param>
    /// <returns>攻击结果</returns>
    public static AttackResult JudgeAttack(AttackType enemyAttack, AttackType? playerAttack)
    {
        // 玩家不出招，直接被击中
        if (playerAttack == null)
        {
            return AttackResult.Hit;
        }

        // 玩家出招与敌人相同，同时攻击
        if (playerAttack.Value == enemyAttack)
        {
            return AttackResult.Clash;
        }

        // 判断玩家是否出了压制招式
        if (IsCounter(enemyAttack, playerAttack.Value))
        {
            return AttackResult.Counter;
        }

        // 其他情况为被击中
        return AttackResult.Hit;
    }

    /// <summary>
    /// 判断玩家攻击是否压制敌人攻击
    /// 压制关系：X克制Y，Y克制B，B克制X
    /// X(突刺)快速击中Y(下压重击)的前摇
    /// Y(下压重击)势大力沉击破B(防御)
    /// B(防御)挡住X(突刺)
    /// </summary>
    /// <param name="enemyAttack">敌人的攻击类型</param>
    /// <param name="playerAttack">玩家的攻击类型</param>
    /// <returns>true表示玩家压制敌人</returns>
    public static bool IsCounter(AttackType enemyAttack, AttackType playerAttack)
    {
        switch (enemyAttack)
        {
            case AttackType.AttackX:
                // 敌人出X(突刺)，玩家出B(防御)可以压制
                return playerAttack == AttackType.AttackB;
                
            case AttackType.AttackY:
                // 敌人出Y(下压)，玩家出X(突刺)可以压制
                return playerAttack == AttackType.AttackX;
                
            case AttackType.AttackB:
                // 敌人出B(防御)，玩家出Y(下压)可以压制
                return playerAttack == AttackType.AttackY;
                
            default:
                return false;
        }
    }

    /// <summary>
    /// 获取压制指定攻击的攻击类型
    /// </summary>
    /// <param name="attackType">敌人的攻击类型</param>
    /// <returns>可以压制该攻击的玩家攻击类型</returns>
    public static AttackType GetCounterAttack(AttackType attackType)
    {
        switch (attackType)
        {
            case AttackType.AttackX:
                return AttackType.AttackB; // B克制X
                
            case AttackType.AttackY:
                return AttackType.AttackX; // X克制Y
                
            case AttackType.AttackB:
                return AttackType.AttackY; // Y克制B
                
            default:
                return AttackType.AttackX;
        }
    }

    /// <summary>
    /// 获取攻击类型的显示名称
    /// </summary>
    public static string GetAttackName(AttackType attackType)
    {
        switch (attackType)
        {
            case AttackType.AttackX:
                return "AttackX(Q键)";
            case AttackType.AttackY:
                return "AttackY(W键)";
            case AttackType.AttackB:
                return "AttackB(E键)";
            default:
                return "Unknown";
        }
    }

    /// <summary>
    /// 获取攻击结果的描述
    /// </summary>
    public static string GetResultDescription(AttackResult result)
    {
        switch (result)
        {
            case AttackResult.Counter:
                return "压制成功！玩家不减血，敌人减血";
            case AttackResult.Clash:
                return "同时攻击！双方都减血";
            case AttackResult.Hit:
                return "被击中！玩家减血，敌人不减血";
            default:
                return "未知结果";
        }
    }
}
