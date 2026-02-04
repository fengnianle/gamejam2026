using UnityEngine;

/// <summary>
/// 游戏日志管理系统
/// 提供统一的日志输出接口，可以通过配置控制不同类型的日志输出
/// 使用方法：
/// 1. 在Hierarchy中创建一个空对象，命名为 "GameLogger"
/// 2. 将此脚本挂载到该对象上
/// 3. 在Inspector中勾选需要输出的日志类型
/// 4. 在其他脚本中使用 GameLogger.Log() 系列方法替代 Debug.Log()
/// </summary>
public class GameLogger : MonoBehaviour
{
    #region 单例模式
    private static GameLogger instance;
    
    public static GameLogger Instance
    {
        get
        {
            if (instance == null)
            {
                // 尝试在场景中查找
                instance = FindObjectOfType<GameLogger>();
                
                // 如果场景中没有，创建一个
                if (instance == null)
                {
                    GameObject loggerObj = new GameObject("GameLogger");
                    instance = loggerObj.AddComponent<GameLogger>();
                    DontDestroyOnLoad(loggerObj);
                    
                    Debug.Log("[GameLogger] 自动创建了GameLogger实例。建议在场景中手动创建以便配置。");
                }
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region 日志类型开关配置
    [Header("=== 全局日志开关 ===")]
    [Tooltip("主开关：关闭后将禁用所有游戏日志输出（Unity系统日志不受影响）")]
    public bool enableLogging = true;

    [Header("=== 战斗系统日志 ===")]
    [Tooltip("调试对峙攻击过程（包含攻击窗口、玩家反制、伤害结算等完整战斗流程日志）")]
    public bool debugCombatProcess = true;
    
    [Tooltip("攻击窗口相关日志（攻击判定窗口开启/关闭、反击判定等）")]
    public bool logAttackWindow = true;
    
    [Tooltip("反制系统相关日志（反制成功/失败、无敌时间等）")]
    public bool logCounter = true;
    
    [Tooltip("伤害系统相关日志（造成伤害、受到伤害、生命值变化等）")]
    public bool logDamage = true;
    
    [Tooltip("死亡相关日志")]
    public bool logDeath = true;

    [Header("=== 动画系统日志 ===")]
    [Tooltip("动画播放相关日志（动画切换、动画事件等）")]
    public bool logAnimation = false;
    
    [Tooltip("动画事件相关日志")]
    public bool logAnimationEvent = false;

    [Header("=== 角色控制日志 ===")]
    [Tooltip("玩家输入和行为日志")]
    public bool logPlayerAction = false;
    
    [Tooltip("Boss行为和AI日志")]
    public bool logBossAction = true;

    [Header("=== 组件验证日志 ===")]
    [Tooltip("组件绑定验证日志（警告和错误）")]
    public bool logComponentValidation = true;

    [Header("=== 通用日志 ===")]
    [Tooltip("一般信息日志")]
    public bool logInfo = true;
    
    [Tooltip("警告日志")]
    public bool logWarning = true;
    
    [Tooltip("错误日志（建议始终开启）")]
    public bool logError = true;
    #endregion

    #region 日志输出方法

    // ==================== 攻击窗口日志 ====================
    
    /// <summary>
    /// 攻击窗口相关日志
    /// </summary>
    public static void LogAttackWindow(string message)
    {
        if (Instance.enableLogging && Instance.logAttackWindow)
        {
            Debug.Log($"<color=orange>[AttackWindow]</color> {message}");
        }
    }

    // ==================== 反制系统日志 ====================
    
    /// <summary>
    /// 反制系统日志
    /// </summary>
    public static void LogCounter(string message)
    {
        if (Instance.enableLogging && Instance.logCounter)
        {
            Debug.Log($"<color=yellow>[Counter]</color> {message}");
        }
    }

    /// <summary>
    /// 反制成功日志
    /// </summary>
    public static void LogCounterSuccess(string actionName, AttackType attackType)
    {
        if (Instance.enableLogging && Instance.logCounter)
        {
            Debug.Log($"<color=lime>[Counter]</color> 🎯 完美反制！使用 {actionName} 反制了 {attackType}");
        }
    }

    /// <summary>
    /// 反制失败日志
    /// </summary>
    public static void LogCounterFail(string reason)
    {
        if (Instance.enableLogging && Instance.logCounter)
        {
            Debug.LogWarning($"<color=yellow>[Counter]</color> ❌ 反制失败：{reason}");
        }
    }

    /// <summary>
    /// 反制窗口开始日志
    /// </summary>
    public static void LogCounterWindowStart(AttackType attackType)
    {
        if (Instance.enableLogging && Instance.logCounter)
        {
            Debug.Log($"<color=yellow>[Counter]</color> ⚡ 敌人发起攻击: {attackType}，等待玩家反制输入...");
        }
    }

    /// <summary>
    /// 无敌时间日志
    /// </summary>
    public static void LogInvincibility(string message)
    {
        if (Instance.enableLogging && Instance.logCounter)
        {
            Debug.Log($"<color=yellow>[Counter]</color> 🛡️ {message}");
        }
    }

    // ==================== 伤害系统日志 ====================
    
    /// <summary>
    /// 伤害系统日志（造成伤害）
    /// </summary>
    public static void LogDamageDealt(string attacker, string target, float damage)
    {
        if (Instance.enableLogging && Instance.logDamage)
        {
            Debug.Log($"<color=red>[Damage]</color> {attacker} 对 {target} 造成 {damage} 点伤害");
        }
    }

    /// <summary>
    /// 伤害系统日志（受到伤害）
    /// </summary>
    public static void LogDamageTaken(string target, float damage, float currentHealth, float maxHealth)
    {
        if (Instance.enableLogging && Instance.logDamage)
        {
            Debug.Log($"<color=red>[Damage]</color> {target} 受到 {damage} 点伤害，当前生命值：{currentHealth}/{maxHealth}");
        }
    }

    /// <summary>
    /// 治疗日志
    /// </summary>
    public static void LogHeal(string target, float amount, float currentHealth, float maxHealth)
    {
        if (Instance.enableLogging && Instance.logDamage)
        {
            Debug.Log($"<color=green>[Heal]</color> {target} 恢复 {amount} 点生命值，当前生命值：{currentHealth}/{maxHealth}");
        }
    }

    /// <summary>
    /// 死亡日志
    /// </summary>
    public static void LogDeath(string target)
    {
        if (Instance.enableLogging && Instance.logDeath)
        {
            Debug.Log($"<color=red>[Death]</color> {target} 已死亡/被击败！");
        }
    }

    // ==================== 动画系统日志 ====================
    
    /// <summary>
    /// 动画播放日志
    /// </summary>
    public static void LogAnimation(string character, string animationName)
    {
        if (Instance.enableLogging && Instance.logAnimation)
        {
            Debug.Log($"<color=cyan>[Animation]</color> {character} 播放动画：{animationName}");
        }
    }

    /// <summary>
    /// 动画事件日志
    /// </summary>
    public static void LogAnimationEvent(string character, string eventName)
    {
        if (Instance.enableLogging && Instance.logAnimationEvent)
        {
            Debug.Log($"<color=cyan>[AnimEvent]</color> {character} 触发动画事件：{eventName}");
        }
    }

    // ==================== 角色控制日志 ====================
    
    /// <summary>
    /// 玩家行为日志
    /// </summary>
    public static void LogPlayerAction(string action)
    {
        if (Instance.enableLogging && Instance.logPlayerAction)
        {
            Debug.Log($"<color=blue>[Player]</color> {action}");
        }
    }

    /// <summary>
    /// Boss行为日志
    /// </summary>
    public static void LogBossAction(string action)
    {
        if (Instance.enableLogging && Instance.logBossAction)
        {
            Debug.Log($"<color=purple>[Boss]</color> {action}");
        }
    }

    // ==================== 组件验证日志 ====================
    
    /// <summary>
    /// 组件验证日志
    /// </summary>
    public static void LogComponentValidation(string message, LogType logType = LogType.Warning)
    {
        if (!Instance.enableLogging || !Instance.logComponentValidation) return;

        switch (logType)
        {
            case LogType.Error:
                Debug.LogError($"<color=red>[Validation]</color> {message}");
                break;
            case LogType.Warning:
                Debug.LogWarning($"<color=yellow>[Validation]</color> {message}");
                break;
            default:
                Debug.Log($"<color=white>[Validation]</color> {message}");
                break;
        }
    }

    // ==================== 通用日志 ====================
    
    /// <summary>
    /// 一般信息日志
    /// </summary>
    public static void Log(string message, string category = "Info")
    {
        if (Instance.enableLogging && Instance.logInfo)
        {
            Debug.Log($"<color=white>[{category}]</color> {message}");
        }
    }

    /// <summary>
    /// 警告日志
    /// </summary>
    public static void LogWarning(string message, string category = "Warning")
    {
        if (Instance.enableLogging && Instance.logWarning)
        {
            Debug.LogWarning($"<color=yellow>[{category}]</color> {message}");
        }
    }

    /// <summary>
    /// 错误日志
    /// </summary>
    public static void LogError(string message, string category = "Error")
    {
        if (Instance.enableLogging && Instance.logError)
        {
            Debug.LogError($"<color=red>[{category}]</color> {message}");
        }
    }

    #endregion

    #region 战斗过程调试日志

    /// <summary>
    /// 战斗过程日志 - 攻击窗口开启
    /// </summary>
    public static void LogCombatAttackWindowStart(string character, AttackType attackType)
    {
        if (!Instance.enableLogging || !Instance.debugCombatProcess) return;
        
        string colorTag = character.Contains("Boss") ? "<color=red>" : "<color=green>";
        Debug.Log($"<color=cyan>[AttackWindow]</color> {colorTag}{character}</color> OnAttackWindow Start");
        Debug.Log($"<color=cyan>[AttackWindow]</color> {colorTag}{character}</color>: 攻击窗口已开启 - 攻击类型: {attackType}");
    }

    /// <summary>
    /// 战斗过程日志 - 攻击窗口关闭
    /// </summary>
    public static void LogCombatAttackWindowEnd(string character, float duration)
    {
        if (!Instance.enableLogging || !Instance.debugCombatProcess) return;
        
        string colorTag = character.Contains("Boss") ? "<color=red>" : "<color=green>";
        Debug.Log($"<color=cyan>[AttackWindow]</color> {colorTag}{character}</color>: 攻击窗口已关闭 - 持续时间: {duration:F2}秒");
        Debug.Log($"<color=cyan>[AttackWindow]</color> {colorTag}{character}</color> OnAttackWindow End");
    }

    /// <summary>
    /// 战斗过程日志 - 等待玩家反制
    /// </summary>
    public static void LogCombatWaitForCounter(AttackType attackType)
    {
        if (!Instance.enableLogging || !Instance.debugCombatProcess) return;
        
        Debug.Log($"<color=yellow>[Counter]</color> ⚡ 敌人发起攻击: {attackType}，等待玩家反制输入...");
    }

    /// <summary>
    /// 战斗过程日志 - 玩家反制操作
    /// </summary>
    public static void LogCombatPlayerCounter(string actionName, AttackRelationship.AttackResult result)
    {
        if (!Instance.enableLogging || !Instance.debugCombatProcess) return;
        
        string resultText = "";
        switch (result)
        {
            case AttackRelationship.AttackResult.Counter:
                resultText = "<color=green>压制成功</color>";
                break;
            case AttackRelationship.AttackResult.Clash:
                resultText = "<color=yellow>同时攻击</color>";
                break;
            case AttackRelationship.AttackResult.Hit:
                resultText = "<color=red>被压制</color>";
                break;
        }
        
        Debug.Log($"<color=yellow>[Counter]</color> <color=green>Player</color> 使用 {actionName}，结果: {resultText}");
    }

    /// <summary>
    /// 战斗过程日志 - 伤害结算
    /// </summary>
    public static void LogCombatDamage(string target, float damage, float currentHealth, float maxHealth)
    {
        if (!Instance.enableLogging || !Instance.debugCombatProcess) return;
        
        string colorTag = target.Contains("Boss") ? "<color=red>" : "<color=green>";
        Debug.Log($"<color=magenta>[Damage]</color> {colorTag}{target}</color> 受到 {damage} 点伤害，当前生命值：{currentHealth}/{maxHealth}");
    }

    /// <summary>
    /// 战斗过程日志 - Boss执行动作
    /// </summary>
    public static void LogCombatBossAction(BossActionType actionType, float duration)
    {
        if (!Instance.enableLogging || !Instance.debugCombatProcess) return;
        
        Debug.Log($"<color=red>[Boss]</color> 执行动作 {actionType}，持续时间 {duration:F1} 秒");
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 启用所有日志
    /// </summary>
    public void EnableAllLogs()
    {
        enableLogging = true;
        logAttackWindow = true;
        logDamage = true;
        logDeath = true;
        logAnimation = true;
        logAnimationEvent = true;
        logPlayerAction = true;
        logBossAction = true;
        logComponentValidation = true;
        logInfo = true;
        logWarning = true;
        logError = true;

        Debug.Log("[GameLogger] 已启用所有日志输出");
    }

    /// <summary>
    /// 禁用所有日志
    /// </summary>
    public void DisableAllLogs()
    {
        enableLogging = false;
        Debug.Log("[GameLogger] 已禁用所有日志输出");
    }

    /// <summary>
    /// 仅启用错误和警告日志
    /// </summary>
    public void EnableErrorAndWarningOnly()
    {
        enableLogging = true;
        logAttackWindow = false;
        logDamage = false;
        logDeath = false;
        logAnimation = false;
        logAnimationEvent = false;
        logPlayerAction = false;
        logBossAction = false;
        logComponentValidation = true;
        logInfo = false;
        logWarning = true;
        logError = true;

        Debug.Log("[GameLogger] 仅启用错误和警告日志");
    }

    /// <summary>
    /// 启用战斗相关日志
    /// </summary>
    public void EnableCombatLogsOnly()
    {
        enableLogging = true;
        logAttackWindow = true;
        logDamage = true;
        logDeath = true;
        logAnimation = false;
        logAnimationEvent = false;
        logPlayerAction = false;
        logBossAction = true;
        logComponentValidation = false;
        logInfo = false;
        logWarning = true;
        logError = true;

        Debug.Log("[GameLogger] 已启用战斗相关日志");
    }

    #endregion
}

/// <summary>
/// 日志类型枚举（用于组件验证等）
/// </summary>
public enum LogType
{
    Log,
    Warning,
    Error
}
