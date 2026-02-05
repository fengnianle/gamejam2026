using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 攻击判定窗口组件（基于时间的输入判定系统）
/// 不使用物理碰撞，而是在动画播放期间检测玩家输入
/// 使用方法：
/// 1. 挂载到Boss对象或子对象上
/// 2. 通过Animation Event调用StartWindow()和EndWindow()控制判定窗口
/// 3. 在窗口期间，系统会检测玩家是否按下了反制键
/// </summary>
public class AttackWindow : MonoBehaviour
{
    [Header("攻击窗口设置")]
    [Tooltip("攻击类型标识（用于判断玩家需要按什么键反制）")]
    public AttackType attackType = AttackType.AttackX;
    
    [Tooltip("攻击伤害值")]
    public float damage = 50f;
    
    [Tooltip("攻击判定窗口持续时间（秒）\n建议值：0.25秒（Unity动画3帧间隔，12fps下）")]
    public float windowDuration = 0.25f;


    [Header("窗口状态")]
    [Tooltip("当前是否在判定窗口内")]
    [SerializeField] private bool isWindowActive = false;
    
    [Tooltip("窗口开始时间")]
    [SerializeField] private float windowStartTime = 0f;
    
    [Tooltip("窗口实际持续时间（运行时计算）")]
    [SerializeField] private float actualWindowDuration = 0f;

    [Header("事件回调")]
    [Tooltip("窗口开启时触发")]
    public UnityEvent onWindowStart;
    
    [Tooltip("窗口关闭时触发")]
    public UnityEvent onWindowEnd;
    
    [Tooltip("玩家成功反制时触发")]
    public UnityEvent<string> onCounterSuccess;
    
    [Tooltip("玩家未反制，攻击命中时触发")]
    public UnityEvent<GameObject> onAttackHit;

    [Header("场景对象引用")]
    [Tooltip("目标对象（Player对象，通过拖拽赋值）")]
    public GameObject targetObject;
    
    [Tooltip("可选：反制成功时播放的Spark特效ParticleSystem（请在Inspector中拖拽赋值）")]
    public ParticleSystem sparkEffectParticle;

    [Header("调试选项")]
    [Tooltip("是否显示调试信息")]
    public bool showDebugInfo = true;

    private bool hasBeenCountered = false; // 是否已被反制

    void Update()
    {
        if (isWindowActive)
        {
            // 检查窗口是否超时
            if (Time.time - windowStartTime >= windowDuration)
            {
                // 窗口结束，如果没有被反制，则造成伤害
                if (!hasBeenCountered)
                {
                    DealDamage();
                }
                EndWindow();
            }
        }
    }

    /// <summary>
    /// 开启政击判定窗口（由Animation Event调用）
    /// </summary>
    public void StartWindow()
    {
        isWindowActive = true;
        windowStartTime = Time.time;
        hasBeenCountered = false;

        // 使用新的战斗过程日志
        string characterName = gameObject.name.Contains("Boss") || transform.parent != null && transform.parent.name.Contains("Boss") ? "Boss" : "Player";
        GameLogger.LogCombatAttackWindowStart(characterName, attackType);
        
        onWindowStart?.Invoke();
        
        // 通知反制系统
        NotifyCounterSystem();
    }

    /// <summary>
    /// 结束攻击判定窗口（由Animation Event调用）
    /// </summary>
    public void EndWindow()
    {
        if (!isWindowActive) return;

        actualWindowDuration = Time.time - windowStartTime;
        isWindowActive = false;
        
        // 使用新的战斗过程日志
        string characterName = gameObject.name.Contains("Boss") || transform.parent != null && transform.parent.name.Contains("Boss") ? "Boss" : "Player";
        GameLogger.LogCombatAttackWindowEnd(characterName, actualWindowDuration);
        
        onWindowEnd?.Invoke();
    }

    /// <summary>
    /// 通知反制系统有新的攻击窗口
    /// </summary>
    void NotifyCounterSystem()
    {
        if (targetObject == null) return;

        // 尝试通知玩家的反制检测器
        var counterDetector = targetObject.GetComponent<CounterInputDetector>();
        if (counterDetector != null)
        {
            counterDetector.OnEnemyAttackStart(attackType, this);
        }
    }

    /// <summary>
    /// 处理玩家攻击响应（由CounterInputDetector调用）
    /// </summary>
    /// <param name="playerAction">玩家的攻击动作名称</param>
    /// <param name="attackResult">攻击结果</param>
    public void OnPlayerResponse(string playerAction, AttackRelationship.AttackResult attackResult)
    {
        if (!isWindowActive) return;

        hasBeenCountered = true;
        
        GameLogger.Log($"玩家使用 {playerAction} 对 {attackType} 做出响应，结果: {AttackRelationship.GetResultDescription(attackResult)}", "Combat");
        
        // 根据攻击结果处理伤害
        HandleDamageByResult(attackResult);
        
        // 如果是压制成功，播放Spark特效
        if (attackResult == AttackRelationship.AttackResult.Counter)
        {
            PlaySparkEffect();
        }
        
        onCounterSuccess?.Invoke(playerAction);
        
        // 立即结束窗口
        EndWindow();
    }
    
    /// <summary>
    /// 处理反制成功（向后兼容的方法）
    /// </summary>
    public void OnCounterSuccess(string playerAction)
    {
        OnPlayerResponse(playerAction, AttackRelationship.AttackResult.Counter);
    }

    /// <summary>
    /// 对目标造成伤害（窗口结束且未被反制时调用）
    /// </summary>
    void DealDamage()
    {
        if (targetObject == null) return;

        // 尝试获取PlayerController或BossController来造成伤害
        var playerController = targetObject.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.TakeDamage(damage);
            GameLogger.LogDamageDealt(gameObject.name, targetObject.name, damage);
            onAttackHit?.Invoke(targetObject);
            return;
        }

        var bossController = targetObject.GetComponent<BossController>();
        if (bossController != null)
        {
            // 检查Boss是否正在攻击窗口内
            var bossAttackWindow = bossController.GetComponent<AttackWindow>();
            if (bossAttackWindow != null && bossAttackWindow.IsWindowActive())
            {
                // Boss正在攻击，这是一次反制动作
                // 伤害已经在Boss的AttackWindow.HandleDamageByResult()中处理了
                // 不应该再次造成伤害，避免双重判定
                GameLogger.Log($"Player攻击触发反制判定，伤害已在Boss攻击窗口中处理，跳过重复判定", "Combat");
                return;
            }
            
            // Boss未出招，玩家攻击静止的Boss，造成低伤害
            var playerCtrl = GetComponentInParent<PlayerController>();
            float idleDamage = damage; // 默认伤害
            
            if (playerCtrl != null && playerCtrl.characterStats != null)
            {
                idleDamage = playerCtrl.characterStats.idleAttackDamage;
            }
            
            bossController.TakeDamage(idleDamage);
            GameLogger.LogDamageDealt(gameObject.name, targetObject.name, idleDamage);
            GameLogger.Log($"玩家攻击静止的Boss，造成低伤害: {idleDamage}", "Combat");
            onAttackHit?.Invoke(targetObject);
            return;
        }

        GameLogger.LogWarning($"AttackWindow: 目标对象 {targetObject.name} 没有可接收伤害的组件！", "AttackWindow");
    }

    /// <summary>
    /// 根据攻击结果处理伤害
    /// </summary>
    void HandleDamageByResult(AttackRelationship.AttackResult result)
    {
        if (targetObject == null) return;

        var playerController = targetObject.GetComponent<PlayerController>();
        var bossController = GetComponentInParent<BossController>(); // Boss是攻击者
        
        if (bossController == null)
        {
            GameLogger.LogError("AttackWindow: 无法找到Boss控制器！", "AttackWindow");
            return;
        }

        switch (result)
        {
            case AttackRelationship.AttackResult.Counter:
                // 压制成功：玩家不减血，Boss减血
                if (bossController != null)
                {
                    float counterDamage = bossController.characterStats != null ? 
                        bossController.characterStats.attackDamage : damage;
                    bossController.TakeDamage(counterDamage);
                    GameLogger.LogDamageDealt("Player", gameObject.name, counterDamage);
                    GameLogger.Log("压制成功！Boss受到伤害，Player安全", "Combat");
                }
                break;
                
            case AttackRelationship.AttackResult.Clash:
                // 同时攻击：双方都减血（使用clashDamage）
                if (playerController != null && bossController != null)
                {
                    float clashDamagePlayer = playerController.characterStats != null ? 
                        playerController.characterStats.clashDamage : damage * 0.8f;
                    float clashDamageBoss = bossController.characterStats != null ? 
                        bossController.characterStats.clashDamage : damage * 0.8f;
                        
                    playerController.TakeDamage(clashDamagePlayer);
                    bossController.TakeDamage(clashDamageBoss);
                    
                    GameLogger.LogDamageDealt("Boss", "Player", clashDamagePlayer);
                    GameLogger.LogDamageDealt("Player", "Boss", clashDamageBoss);
                    GameLogger.Log("同时攻击！双方都受到伤害", "Combat");
                }
                break;
                
            case AttackRelationship.AttackResult.Hit:
                // 被击中：玩家减血，Boss不减血
                if (playerController != null)
                {
                    playerController.TakeDamage(damage);
                    GameLogger.LogDamageDealt(gameObject.name, targetObject.name, damage);
                    GameLogger.Log("玩家被击中！受到伤害", "Combat");
                }
                break;
        }
    }

    /// <summary>
    /// 设置伤害值
    /// </summary>
    public void SetDamage(float newDamage)
    {
        damage = newDamage;
    }

    /// <summary>
    /// 设置攻击类型
    /// </summary>
    public void SetAttackType(AttackType type)
    {
        attackType = type;
    }
    
    /// <summary>
    /// 获取当前攻击类型
    /// </summary>
    public AttackType GetAttackType()
    {
        return attackType;
    }

    /// <summary>
    /// 获取当前窗口是否激活
    /// </summary>
    public bool IsWindowActive()
    {
        return isWindowActive;
    }

    /// <summary>
    /// 获取窗口剩余时间
    /// </summary>
    public float GetRemainingTime()
    {
        if (!isWindowActive) return 0f;
        float elapsed = Time.time - windowStartTime;
        return Mathf.Max(0f, windowDuration - elapsed);
    }

    /// <summary>
    /// 在Scene视图中绘制调试信息
    /// </summary>
    void OnDrawGizmos()
    {
        if (!showDebugInfo) return;

        // 显示窗口状态
        Gizmos.color = isWindowActive ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }

    /// <summary>
    /// 播放Spark特效
    /// </summary>
    void PlaySparkEffect()
    {
        if (sparkEffectParticle != null)
        {
            // 播放粒子特效
            sparkEffectParticle.Play();
            GameLogger.Log("播放Spark反制成功特效", "Combat");
        }
    }
}

/// <summary>
/// 攻击类型枚举
/// </summary>
public enum AttackType
{
    AttackX,    // 攻击X - 对应Q键反制
    AttackY,    // 攻击Y - 对应W键反制
    AttackB     // 攻击B - 对应E键反制
}
