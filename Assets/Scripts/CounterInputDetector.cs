using UnityEngine;

/// <summary>
/// 反制输入检测器
/// 挂载在Player对象上，检测玩家在敌人攻击窗口内的输入
/// 如果玩家按下了正确的反制键，则触发反制成功
/// </summary>
public class CounterInputDetector : MonoBehaviour
{
    [Header("反制设置")]
    [Tooltip("反制成功时的奖励伤害倍数")]
    public float counterDamageMultiplier = 2f;
    
    [Tooltip("反制成功后的无敌时间")]
    public float invincibilityTime = 0.5f;

    [Header("按键映射")]
    [Tooltip("反制攻击1的按键")]
    public KeyCode counterAttack1Key = KeyCode.Q;
    
    [Tooltip("反制攻击2的按键")]
    public KeyCode counterAttack2Key = KeyCode.W;
    
    [Tooltip("反制攻击3的按键")]
    public KeyCode counterAttack3Key = KeyCode.E;

    [Header("状态")]
    [SerializeField] private AttackWindow currentAttackWindow;
    [SerializeField] private AttackType expectedAttackType;
    [SerializeField] private bool isWaitingForInput = false;
    [SerializeField] private bool isInvincible = false;
    private float invincibilityEndTime = 0f;

    [Header("UI反馈（可选）")]
    [Tooltip("显示反制提示的UI对象")]
    public GameObject counterPromptUI;

    void Update()
    {
        // 检查无敌时间
        if (isInvincible && Time.time >= invincibilityEndTime)
        {
            isInvincible = false;
            GameLogger.Log("Player无敌时间结束", "Counter");
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

        GameLogger.Log($"敌人发起攻击: {attackType}，等待玩家反制输入...", "Counter");

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
            TryCounter(AttackType.Attack1, actionName);
        }
        else if (Input.GetKeyDown(counterAttack2Key))
        {
            pressedKey = counterAttack2Key;
            actionName = "W键反制";
            TryCounter(AttackType.Attack2, actionName);
        }
        else if (Input.GetKeyDown(counterAttack3Key))
        {
            pressedKey = counterAttack3Key;
            actionName = "E键反制";
            TryCounter(AttackType.Attack3, actionName);
        }
    }

    /// <summary>
    /// 尝试进行反制
    /// </summary>
    void TryCounter(AttackType playerInput, string actionName)
    {
        if (currentAttackWindow == null || !currentAttackWindow.IsWindowActive())
        {
            GameLogger.LogWarning("反制失败：不在攻击窗口内", "Counter");
            OnCounterFail();
            return;
        }

        // 检查按键是否正确
        if (playerInput == expectedAttackType)
        {
            // 反制成功！
            OnCounterSuccess(actionName);
        }
        else
        {
            // 按错了键
            GameLogger.LogWarning($"反制失败：按键错误（期望: {expectedAttackType}, 实际: {playerInput}）", "Counter");
            OnCounterFail();
        }
    }

    /// <summary>
    /// 反制成功处理
    /// </summary>
    void OnCounterSuccess(string actionName)
    {
        GameLogger.Log($"🎯 完美反制！使用 {actionName}", "Counter");

        // 通知攻击窗口反制成功
        if (currentAttackWindow != null)
        {
            currentAttackWindow.OnCounterSuccess(actionName);
        }

        // 进入无敌状态
        isInvincible = true;
        invincibilityEndTime = Time.time + invincibilityTime;

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
        expectedAttackType = AttackType.Attack1;
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
