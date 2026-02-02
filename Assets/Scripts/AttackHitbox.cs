using UnityEngine;

/// <summary>
/// 攻击判定区域组件
/// 负责检测攻击碰撞并造成伤害
/// 使用方法：
/// 1. 在角色子对象上添加此脚本
/// 2. 添加Collider2D组件并勾选Is Trigger
/// 3. 通过Animation Event调用EnableHitbox()和DisableHitbox()控制攻击判定窗口
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class AttackHitbox : MonoBehaviour
{
    [Header("攻击设置")]
    [Tooltip("攻击伤害值")]
    public float damage = 10f;
    
    [Tooltip("可以被攻击的对象标签（例如：Player 或 Boss）")]
    public string targetTag = "Enemy";
    
    [Tooltip("可以被攻击的层级（Layer）")]
    public LayerMask hitLayer;

    [Header("调试选项")]
    [Tooltip("是否显示调试信息")]
    public bool showDebugInfo = true;

    [Header("内部引用")]
    private Collider2D hitboxCollider;
    private bool isActive = false;

    // 用于防止同一目标被重复击中
    private GameObject lastHitTarget = null;
    private float lastHitTime = 0f;
    private float hitCooldown = 0.1f; // 同一目标的最小击中间隔

    void Awake()
    {
        // 获取碰撞体组件
        hitboxCollider = GetComponent<Collider2D>();
        
        if (hitboxCollider == null)
        {
            GameLogger.LogError($"AttackHitbox ({gameObject.name}): 未找到Collider2D组件！", "AttackHitbox");
            return;
        }

        // 确保碰撞体是触发器
        if (!hitboxCollider.isTrigger)
        {
            GameLogger.LogWarning($"AttackHitbox ({gameObject.name}): Collider2D未设置为Trigger，已自动设置。", "AttackHitbox");
            hitboxCollider.isTrigger = true;
        }

        // 初始时禁用碰撞体
        DisableHitbox();
    }

    /// <summary>
    /// 启用攻击判定区域（由Animation Event调用）
    /// </summary>
    public void EnableHitbox()
    {
        if (hitboxCollider == null) return;

        isActive = true;
        hitboxCollider.enabled = true;
        lastHitTarget = null; // 重置上次击中的目标
        
        GameLogger.LogAttackHitbox($"{gameObject.name}: 攻击判定已启用");
    }

    /// <summary>
    /// 禁用攻击判定区域（由Animation Event调用）
    /// </summary>
    public void DisableHitbox()
    {
        if (hitboxCollider == null) return;

        isActive = false;
        hitboxCollider.enabled = false;
        lastHitTarget = null;
        
        GameLogger.LogAttackHitbox($"{gameObject.name}: 攻击判定已禁用");
    }

    /// <summary>
    /// 当其他碰撞体进入触发区域时调用
    /// </summary>
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return;

        // 检查是否是可攻击的目标
        if (!IsValidTarget(other.gameObject)) return;

        // 防止短时间内重复击中同一目标
        if (other.gameObject == lastHitTarget && Time.time - lastHitTime < hitCooldown)
        {
            return;
        }

        // 造成伤害
        DealDamage(other.gameObject);

        // 记录击中信息
        lastHitTarget = other.gameObject;
        lastHitTime = Time.time;
    }

    /// <summary>
    /// 检查是否是有效的攻击目标
    /// </summary>
    bool IsValidTarget(GameObject target)
    {
        // 检查标签
        bool hasCorrectTag = !string.IsNullOrEmpty(targetTag) && target.CompareTag(targetTag);
        
        // 检查Layer
        bool isInHitLayer = ((1 << target.layer) & hitLayer) != 0;

        return hasCorrectTag || isInHitLayer;
    }

    /// <summary>
    /// 对目标造成伤害
    /// </summary>
    void DealDamage(GameObject target)
    {
        // 尝试调用目标的TakeDamage方法
        var damageable = target.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            GameLogger.LogDamageDealt(gameObject.name, target.name, damage);
            return;
        }

        // 如果没有IDamageable接口，尝试通过SendMessage发送伤害
        target.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        GameLogger.LogDamageDealt(gameObject.name, target.name, damage);
    }

    /// <summary>
    /// 设置伤害值
    /// </summary>
    public void SetDamage(float newDamage)
    {
        damage = newDamage;
    }

    /// <summary>
    /// 设置目标标签
    /// </summary>
    public void SetTargetTag(string tag)
    {
        targetTag = tag;
    }

    /// <summary>
    /// 在Scene视图中绘制Gizmos（便于调试）
    /// </summary>
    void OnDrawGizmos()
    {
        if (!showDebugInfo) return;

        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        // 根据是否激活设置不同颜色
        Gizmos.color = isActive ? Color.red : Color.green;

        // 绘制碰撞体轮廓
        if (col is BoxCollider2D boxCol)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(boxCol.offset, boxCol.size);
        }
        else if (col is CircleCollider2D circleCol)
        {
            Gizmos.DrawWireSphere(transform.position + (Vector3)circleCol.offset, circleCol.radius);
        }
    }
}

/// <summary>
/// 可受伤对象接口
/// 实现此接口的对象可以接收伤害
/// </summary>
public interface IDamageable
{
    void TakeDamage(float damage);
}
