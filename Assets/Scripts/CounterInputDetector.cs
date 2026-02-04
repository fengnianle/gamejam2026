using UnityEngine;

/// <summary>
/// 反制输入检测器
/// 挂载在Player对象上，检测玩家在敌人攻击窗口内的输入
/// 如果玩家按下了正确的反制键，则触发反制成功
/// </summary>
public class CounterInputDetector : MonoBehaviour
{
    [Space(10)]
    [Header("⚠️ 场景对象引用 - 可选配置 ⚠️")]
    [Space(5)]
    [Tooltip("可选：显示反制提示的UI对象（请在Inspector中拖拽赋值）")]
    public GameObject counterPromptUI;

    [Space(10)]
    [Header("反制设置")]
    [Tooltip("反制成功时的奖励伤害倍数")]
    public float counterDamageMultiplier = 2f;
    
    [Tooltip("反制成功后的无敌时间")]
    public float invincibilityTime = 0.5f;

    [Header("按键映射")]
    [Tooltip("反制攻击X的按键")]
    public KeyCode counterAttack1Key = KeyCode.Q;
    
    [Tooltip("反制攻击Y的按键")]
    public KeyCode counterAttack2Key = KeyCode.W;
    
    [Tooltip("反制攻击B的按键")]
    public KeyCode counterAttack3Key = KeyCode.E;

    [Header("状态")]
    [SerializeField] private AttackWindow currentAttackWindow;
    [SerializeField] private AttackType expectedAttackType;
    [SerializeField] private bool isWaitingForInput = false;
    [SerializeField] private bool isInvincible = false;
    private float invincibilityEndTime = 0f;

    void Update()
    {
        // 如果组件被禁用，不进行任何检测（玩家死亡时会禁用此组件）
        if (!enabled) return;
        
        // 检查无敌时间
        if (isInvincible && Time.time >= invincibilityEndTime)
        {
            isInvincible = false;
            GameLogger.LogInvincibility("Player无敌时间结束");
        }

        // 如果正在等待输入，检测按键
        if (isWaitingForInput && currentAttackWindow != null)
        {
            CheckCounterInput();
        }
    }

    /// <summary>
    /// 敌人攻击开始时调用（由AttackWindow通知）
    /// </summary>
    public void OnEnemyAttackStart(AttackType attackType, AttackWindow attackWindow)
    {
        currentAttackWindow = attackWindow;
        expectedAttackType = attackType;
        isWaitingForInput = true;

        // 使用新的战斗过程日志
        GameLogger.LogCombatWaitForCounter(attackType);

        // 显示反制提示UI
        ShowCounterPrompt(attackType);
    }

    /// <summary>
    /// 检测玩家的反制输入
    /// </summary>
    void CheckCounterInput()
    {
        KeyCode pressedKey = KeyCode.None;
        string actionName = "";

        // 检测玩家按下了哪个键
        if (Input.GetKeyDown(counterAttack1Key))
        {
            pressedKey = counterAttack1Key;
            actionName = "Q键反制";
            TryCounter(AttackType.AttackX, actionName);
        }
        else if (Input.GetKeyDown(counterAttack2Key))
        {
            pressedKey = counterAttack2Key;
            actionName = "W键反制";
            TryCounter(AttackType.AttackY, actionName);
        }
        else if (Input.GetKeyDown(counterAttack3Key))
        {
            pressedKey = counterAttack3Key;
            actionName = "E键反制";
            TryCounter(AttackType.AttackB, actionName);
        }
    }

    /// <summary>
    /// 尝试进行反制
    /// </summary>
    void TryCounter(AttackType playerInput, string actionName)
    {
        if (currentAttackWindow == null || !currentAttackWindow.IsWindowActive())
        {
            GameLogger.LogCounterFail("不在攻击窗口内");
            OnCounterFail();
            return;
        }

        // 使用AttackRelationship判定攻击结果
        AttackRelationship.AttackResult result = AttackRelationship.JudgeAttack(expectedAttackType, playerInput);
        
        // 根据结果处理
        switch (result)
        {
            case AttackRelationship.AttackResult.Counter:
                // 压制成功
                GameLogger.LogCounterSuccess(actionName, expectedAttackType);
                GameLogger.Log($"压制成功！{AttackRelationship.GetAttackName(playerInput)} 压制了 {AttackRelationship.GetAttackName(expectedAttackType)}", "Combat");
                OnPlayerAction(actionName, result);
                break;
                
            case AttackRelationship.AttackResult.Clash:
                // 同时攻击
                GameLogger.Log($"同时攻击！玩家和Boss都使用了 {AttackRelationship.GetAttackName(playerInput)}", "Combat");
                OnPlayerAction(actionName, result);
                break;
                
            case AttackRelationship.AttackResult.Hit:
                // 被压制
                GameLogger.LogCounterFail($"被压制！{AttackRelationship.GetAttackName(playerInput)} 被 {AttackRelationship.GetAttackName(expectedAttackType)} 压制");
                OnPlayerAction(actionName, result);
                break;
        }
    }

    /// <summary>
    /// 反制成功处理
    /// </summary>
    void OnCounterSuccess(string actionName)
    {
        GameLogger.LogCounterSuccess(actionName, expectedAttackType);

        // 通知攻击窗口反制成功
        if (currentAttackWindow != null)
        {
            currentAttackWindow.OnCounterSuccess(actionName);
        }

        // 进入无敌状态
        isInvincible = true;
        invincibilityEndTime = Time.time + invincibilityTime;
        GameLogger.LogInvincibility($"Player进入无敌状态，持续 {invincibilityTime} 秒");

        // 可以在这里添加：
        // - 播放反制成功动画
        // - 播放反制音效
        // - 显示反制成功特效
        // - 对敌人造成反击伤害

        // 重置状态
        ResetCounterState();
        
        // 隐藏UI提示
        HideCounterPrompt();
    }

    /// <summary>
    /// 处理玩家攻击响应（新方法）
    /// </summary>
    void OnPlayerAction(string actionName, AttackRelationship.AttackResult result)
    {
        // 输出玩家操作日志
        GameLogger.LogCombatPlayerCounter(actionName, result);
        
        // 通知攻击窗口玩家的响应结果
        if (currentAttackWindow != null)
        {
            currentAttackWindow.OnPlayerResponse(actionName, result);
        }

        // 只有压制成功时才进入无敌状态
        if (result == AttackRelationship.AttackResult.Counter)
        {
            isInvincible = true;
            invincibilityEndTime = Time.time + invincibilityTime;
            GameLogger.LogInvincibility($"Player压制成功，进入无敌状态，持续 {invincibilityTime} 秒");
        }

        // 可以在这里根据结果添加不同的效果：
        // - 压制成功：播放反制成功动画、音效、特效
        // - 同时攻击：播放碰撞音效、震屏效果
        // - 被压制：播放受击动画、音效

        // 重置状态
        ResetCounterState();
        
        // 隐藏UI提示
        HideCounterPrompt();
    }

    /// <summary>
    /// 反制失败处理
    /// </summary>
    void OnCounterFail()
    {
        // 可以在这里添加：
        // - 播放失败音效
        // - 显示失败提示

        // 注意：不重置状态，玩家还可以继续尝试
        // 只有当窗口关闭或成功反制后才重置
    }

    /// <summary>
    /// 重置反制状态
    /// </summary>
    void ResetCounterState()
    {
        isWaitingForInput = false;
        currentAttackWindow = null;
        expectedAttackType = AttackType.AttackX;
    }

    /// <summary>
    /// 显示反制提示UI
    /// </summary>
    void ShowCounterPrompt(AttackType attackType)
    {
        if (counterPromptUI != null)
        {
            counterPromptUI.SetActive(true);
            
            // 可以根据攻击类型显示不同的提示
            // 例如：更新UI文本显示 "按Q键反制！"
        }
    }

    /// <summary>
    /// 隐藏反制提示UI
    /// </summary>
    void HideCounterPrompt()
    {
        if (counterPromptUI != null)
        {
            counterPromptUI.SetActive(false);
        }
    }

    /// <summary>
    /// 检查是否处于无敌状态
    /// </summary>
    public bool IsInvincible()
    {
        return isInvincible;
    }

    /// <summary>
    /// 获取当前是否在等待反制输入
    /// </summary>
    public bool IsWaitingForInput()
    {
        return isWaitingForInput;
    }
}
