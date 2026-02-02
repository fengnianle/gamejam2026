using UnityEngine;

/// <summary>
/// 玩家控制器
/// 详细的组件绑定说明请查看项目根目录的 README.md 文件
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour, IDamageable
{
    [Header("动画绑定")]
    [Tooltip("待机动画片段")]
    public AnimationClip idleAnimation;
    
    [Tooltip("攻击1动画片段（Q键触发）")]
    public AnimationClip attackXAnimation;
    
    [Tooltip("攻击2动画片段（W键触发）")]
    public AnimationClip attackYAnimation;
    
    [Tooltip("攻击3动画片段（E键触发）")]
    public AnimationClip attackBAnimation;

    [Header("攻击判定设置")]
    [Tooltip("攻击判定窗口对象（通常是子对象上的AttackWindow组件）")]
    public AttackWindow attackWindow;
    
    [Tooltip("攻击伤害值")]
    public float attackDamage = 10f;

    [Header("反制系统")]
    [Tooltip("反制输入检测器（自动获取）")]
    private CounterInputDetector counterDetector;

    [Header("生命值设置")]
    [Tooltip("最大生命值")]
    public float maxHealth = 100f;
    
    [Tooltip("当前生命值")]
    public float currentHealth = 100f;

    [Header("组件引用")]
    private Animator animator;

    [Header("状态")]
    private bool isAttacking = false;

    void Start()
    {
        // 获取Animator组件
        animator = GetComponent<Animator>();

        // 验证组件和动画绑定
        ComponentValidator.ValidateAnimator(animator, "PlayerController");
        ComponentValidator.ValidateAnimationClips("PlayerController", 
            idleAnimation, attackXAnimation, attackYAnimation, attackBAnimation);

        // 验证攻击判定窗口
        if (attackWindow == null)
        {
            GameLogger.LogWarning("PlayerController: 未绑定AttackWindow组件！攻击将无法造成伤害。", "PlayerController");
        }
        else
        {
            // 设置攻击判定的伤害值
            attackWindow.SetDamage(attackDamage);
        }

        // 获取或添加反制检测器
        counterDetector = GetComponent<CounterInputDetector>();
        if (counterDetector == null)
        {
            counterDetector = gameObject.AddComponent<CounterInputDetector>();
            GameLogger.Log("PlayerController: 自动添加了CounterInputDetector组件", "PlayerController");
        }

        // 初始化生命值
        currentHealth = maxHealth;

        // 初始播放Idle动画
        PlayIdleAnimation();
    }

    void Update()
    {
        // 处理攻击输入
        HandleAttackInput();
    }

    /// <summary>
    /// 处理攻击输入
    /// </summary>
    void HandleAttackInput()
    {
        if (isAttacking)
            return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            PerformAttack(attackXAnimation);
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            PerformAttack(attackYAnimation);
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            PerformAttack(attackBAnimation);
        }
    }

    /// <summary>
    /// 执行攻击
    /// </summary>
    void PerformAttack(AnimationClip attackClip)
    {
        if (!ComponentValidator.CanPlayAnimation(animator, attackClip)) return;

        isAttacking = true;
        animator.Play(attackClip.name);
        Invoke(nameof(ResetAttackState), attackClip.length);
    }

    /// <summary>
    /// 重置攻击状态并返回Idle
    /// </summary>
    void ResetAttackState()
    {
        isAttacking = false;
        PlayIdleAnimation();
    }

    /// <summary>
    /// 播放Idle动画
    /// </summary>
    void PlayIdleAnimation()
    {
        if (ComponentValidator.CanPlayAnimation(animator, idleAnimation))
        {
            animator.Play(idleAnimation.name);
        }
    }

    /// <summary>
    /// 公共方法：停止攻击（可在动画事件中调用）
    /// </summary>
    public void OnAttackComplete()
    {
        isAttacking = false;
    }

    // ==================== 攻击判定窗口控制 ====================
    
    /// <summary>
    /// 开启攻击判定窗口（由Animation Event调用）
    /// 在攻击动画的合适帧添加此Event
    /// </summary>
    public void OnAttackHitboxStart()
    {
        GameLogger.LogAnimationEvent("Player", "OnAttackHitboxStart");
        
        if (attackWindow != null)
        {
            attackWindow.StartWindow();
        }
        else
        {
            GameLogger.LogWarning("PlayerController: AttackWindow未绑定，无法启用攻击判定！", "PlayerController");
        }
    }

    /// <summary>
    /// 关闭攻击判定窗口（由Animation Event调用）
    /// 在攻击动画结束前的合适帧添加此Event
    /// </summary>
    public void OnAttackHitboxEnd()
    {
        GameLogger.LogAnimationEvent("Player", "OnAttackHitboxEnd");
        
        if (attackWindow != null)
        {
            attackWindow.EndWindow();
        }
    }

    // ==================== 受伤系统 ====================
    
    /// <summary>
    /// 接收伤害（实现IDamageable接口）
    /// </summary>
    public void TakeDamage(float damage)
    {
        // 检查是否处于反制无敌状态
        if (counterDetector != null && counterDetector.IsInvincible())
        {
            GameLogger.Log("Player处于无敌状态，免疫伤害！", "Counter");
            return;
        }

        currentHealth -= damage;
        GameLogger.LogDamageTaken("Player", damage, currentHealth, maxHealth);

        // 触发受伤效果（可以在这里添加受伤动画、音效等）
        OnDamaged();

        // 检查是否死亡
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 受伤时的响应
    /// </summary>
    void OnDamaged()
    {
        // 可以在这里添加：
        // - 播放受伤动画
        // - 播放受伤音效
        // - 显示受伤特效
        // - 触发短暂的无敌时间
    }

    /// <summary>
    /// 死亡处理
    /// </summary>
    void Die()
    {
        GameLogger.LogDeath("Player");
        
        // 可以在这里添加：
        // - 播放死亡动画
        // - 禁用控制
        // - 显示游戏结束界面
        // - 重置关卡
        
        // 暂时禁用脚本
        enabled = false;
    }

    /// <summary>
    /// 恢复生命值
    /// </summary>
    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        GameLogger.LogHeal("Player", amount, currentHealth, maxHealth);
    }
}
