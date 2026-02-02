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
    public AttackType attackType = AttackType.Attack1;
    
    [Tooltip("攻击伤害值")]
    public float damage = 10f;
    
    [Tooltip("目标对象（Player对象，通过拖拽赋值）")]
    public GameObject targetObject;

    [Header("窗口状态")]
    [Tooltip("当前是否在判定窗口内")]
    [SerializeField] private bool isWindowActive = false;
    
    [Tooltip("窗口开始时间")]
    [SerializeField] private float windowStartTime = 0f;
    
    [Tooltip("窗口持续时间")]
    [SerializeField] private float windowDuration = 0f;

    [Header("事件回调")]
    [Tooltip("窗口开启时触发")]
    public UnityEvent onWindowStart;
    
    [Tooltip("窗口关闭时触发")]
    public UnityEvent onWindowEnd;
    
    [Tooltip("玩家成功反制时触发")]
    public UnityEvent<string> onCounterSuccess;
    
    [Tooltip("玩家未反制，攻击命中时触发")]
    public UnityEvent<GameObject> onAttackHit;

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
    /// 开启攻击判定窗口（由Animation Event调用）
    /// </summary>
    public void StartWindow()
    {
        isWindowActive = true;
        windowStartTime = Time.time;
        hasBeenCountered = false;

        GameLogger.LogAttackWindow($"{gameObject.name}: 攻击窗口已开启 - 攻击类型: {attackType}");
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

        windowDuration = Time.time - windowStartTime;
        isWindowActive = false;
        
        GameLogger.LogAttackWindow($"{gameObject.name}: 攻击窗口已关闭 - 持续时间: {windowDuration:F2}秒");
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
    /// 处理反制成功（由CounterInputDetector调用）
    /// </summary>
    public void OnCounterSuccess(string playerAction)
    {
        if (!isWindowActive) return;

        hasBeenCountered = true;
        GameLogger.Log($"反制成功！玩家使用 {playerAction} 反制了 {attackType}", "Counter");
        
        onCounterSuccess?.Invoke(playerAction);
        
        // 立即结束窗口
        EndWindow();
    }

    /// <summary>
    /// 对目标造成伤害（窗口结束且未被反制时调用）
    /// </summary>
    void DealDamage()
    {
        if (targetObject == null) return;

GameLogger.LogDamageDealt(gameObject.name, targetObject.name, damage);
        // var damageable = targetObject.GetComponent<IDamageable>();
        // if (damageable != null)
        // {
        //     damageable.TakeDamage(damage);
        //     GameLogger.LogDamageDealt(gameObject.name, targetObject.name, damage);
        //     onAttackHit?.Invoke(targetObject);
        // }
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
        return windowDuration - (Time.time - windowStartTime);
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
}

/// <summary>
/// 攻击类型枚举
/// </summary>
public enum AttackType
{
    Attack1,    // 攻击1 - 对应Q键反制
    Attack2,    // 攻击2 - 对应W键反制
    Attack3     // 攻击3 - 对应E键反制
}
