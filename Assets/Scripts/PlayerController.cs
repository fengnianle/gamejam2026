using UnityEngine;

/// <summary>
/// 玩家控制器
/// 详细的组件绑定说明请查看项目根目录的 README.md 文件
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AttackWindow))]
[RequireComponent(typeof(CounterInputDetector))]
public class PlayerController : MonoBehaviour
{
    /// <summary>
    /// 动画绑定
    /// </summary>
    [Header("动画绑定")]
    [Tooltip("待机动画片段")]
    public AnimationClip idleAnimation;
    
    [Tooltip("攻击X动画片段（Q键触发）")]
    public AnimationClip attackXAnimation;
    
    [Tooltip("攻击Y动画片段（W键触发）")]
    public AnimationClip attackYAnimation;
    
    [Tooltip("攻击B动画片段（E键触发）")]
    public AnimationClip attackBAnimation;
    
    /// <summary>
    /// 定力值伤害
    /// </summary>
    [Header("定力值系统")]
    [Tooltip("最大定力值")]
    public float maxHealth = 100f;
    
    [Tooltip("当前定力值")]
    public float currentHealth = 100f;

    [Tooltip("攻击力")]
    public float attackDamage = 10f;
    
    /// <summary>
    /// 组件获取
    /// </summary>
    [Header("组件引用")]
    [Tooltip("动画控制器（自动获取）")]
    private Animator animator;
    [Tooltip("攻击判定窗口对象（自动获取）")]
    private AttackWindow attackWindow;
    [Tooltip("反制输入检测器（自动获取）")]
    private CounterInputDetector counterDetector;

    /// <summary>
    /// 私有变量
    /// </summary>
    [Header("状态")]
    private bool isAttacking = false;

    /// <summary> ----------------------------------------- 生命周期 ----------------------------------------- </summary>
    void Start()
    {
        // 获取组件引用
        animator = GetComponent<Animator>();
        attackWindow = GetComponent<AttackWindow>();
        counterDetector = GetComponent<CounterInputDetector>();

        Initialized();
        
        PlayIdleAnimation();    // 初始播放Idle动画
    }

    void Update()
    {
        // 检测处理攻击输入
        HandleAttackInput();
    }

    /// <summary> ----------------------------------------- Public  ----------------------------------------- </summary>
    /// <summary>
    /// 公共方法：停止攻击可进行下一次攻击（可在动画事件中调用）
    /// </summary>
    public void OnAttackComplete()
    {
        isAttacking = false;
    }

    // ==================== 攻击判定窗口控制 ====================
    /// <summary>
    /// 开启攻击判定窗口（由Animation Event调用）
    /// </summary>
    public void OnAttackWindowStart()
    {
        GameLogger.LogAttackWindow("Player OnAttackWindow Start");
        
        attackWindow.StartWindow();
    }

    /// <summary>
    /// 关闭攻击判定窗口（由Animation Event调用）
    /// </summary>
    public void OnAttackWindowEnd()
    {
        GameLogger.LogAttackWindow("Player OnAttackWindow End");

        attackWindow.EndWindow();
    }

    // ==================== 受伤系统 ====================
    /// <summary>
    /// 接收伤害
    /// </summary>
    public void TakeDamage(float damage)
    {
        // 检查是否处于反制无敌状态
        // if (counterDetector != null && counterDetector.IsInvincible())
        // {
        //     GameLogger.Log("Player处于无敌状态，免疫伤害！", "Counter");
        //     return;
        // }

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

    /// <summary> ----------------------------------------- Private ----------------------------------------- </summary>
    void Initialized()
    {
        attackWindow.SetDamage(attackDamage);    // 设置攻击判定的伤害值
        currentHealth = maxHealth;  // 初始化生命值
    }

    /// <summary>
    /// 处理攻击输入
    /// </summary>
    void HandleAttackInput()
    {
        if (isAttacking)
        {
            GameLogger.Log("Player当前正在攻击，忽略新的攻击输入", "PlayerController");
            return;
        }

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
    // public void Heal(float amount)
    // {
    //     currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    //     GameLogger.LogHeal("Player", amount, currentHealth, maxHealth);
    // }
    
}
