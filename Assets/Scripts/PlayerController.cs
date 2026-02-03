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
    
    [Tooltip("死亡动画片段")]
    public AnimationClip deathAnimation;
    
    /// <summary>
    /// 角色配置
    /// </summary>
    [Header("角色配置")]
    [Tooltip("角色属性配置（ScriptableObject）")]
    public CharacterStats characterStats;
    
    /// <summary>
    /// 运行时状态（不可序列化，不会保存到场景）
    /// </summary>
    [Header("运行时状态")]
    [Tooltip("当前生命值（运行时动态计算，不保存）")]
    private float currentHealth;
    
    [Header("场景对象引用")]
    [Tooltip("玩家血条UI（请在Inspector中拖拽赋值）")]
    public HPBar hpBar;
    
    [Tooltip("Boss控制器（请在Inspector中拖拽赋值）")]
    public BossController bossController;
    
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
    void Awake()
    {
        // 在Awake中获取组件，确保最早获取
        animator = GetComponent<Animator>();
        attackWindow = GetComponent<AttackWindow>();
        counterDetector = GetComponent<CounterInputDetector>();
        
        GameLogger.Log($"[Awake] GameObject: {gameObject.name}, InstanceID: {GetInstanceID()}", "PlayerController");
        GameLogger.Log($"[Awake] animator: {(animator != null ? "OK" : "NULL")}, attackWindow: {(attackWindow != null ? "OK" : "NULL")}, counterDetector: {(counterDetector != null ? "OK" : "NULL")}", "PlayerController");
    }

    void Start()
    {
        GameLogger.Log($"[Start] GameObject: {gameObject.name}, InstanceID: {GetInstanceID()}", "PlayerController");
        GameLogger.Log($"[Start] bossController: {(bossController != null ? bossController.gameObject.name : "NULL")}", "PlayerController");
        
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
        GameLogger.Log($"========== [TakeDamage] 开始 ==========", "PlayerController");
        GameLogger.Log($"[TakeDamage] GameObject: {gameObject.name}, InstanceID: {GetInstanceID()}, enabled: {enabled}", "PlayerController");
        GameLogger.Log($"[TakeDamage] 当前 currentHealth: {currentHealth}, 即将扣除: {damage}", "PlayerController");
        GameLogger.Log($"[TakeDamage] animator: {(animator != null ? "OK" : "NULL")}", "PlayerController");
        GameLogger.Log($"[TakeDamage] attackWindow: {(attackWindow != null ? "OK" : "NULL")}", "PlayerController");
        GameLogger.Log($"[TakeDamage] counterDetector: {(counterDetector != null ? "OK" : "NULL")}", "PlayerController");
        GameLogger.Log($"[TakeDamage] bossController: {(bossController != null ? bossController.gameObject.name : "NULL")}", "PlayerController");
        GameLogger.Log($"[TakeDamage] characterStats: {(characterStats != null ? characterStats.characterName : "NULL")}", "PlayerController");
        
        // 检查是否处于反制无敌状态
        if (counterDetector != null && counterDetector.IsInvincible())
        {
            GameLogger.LogInvincibility("Player处于无敌状态，免疫伤害！");
            return;
        }

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth); // 确保不会小于0
        
        GameLogger.LogDamageTaken("Player", damage, currentHealth, characterStats.maxHealth);

        // 更新血条显示
        if (hpBar != null)
        {
            hpBar.UpdateHP(currentHealth, characterStats.maxHealth);
        }

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
        GameLogger.Log($"[Initialized] 开始初始化", "PlayerController");
        
        // 验证配置
        if (characterStats == null)
        {
            GameLogger.LogError("CharacterStats未赋值！请在Inspector中拖拽赋值", "PlayerController");
            return;
        }
        
        // 从配置中读取初始值
        currentHealth = characterStats.maxHealth;  // 初始化生命值
        attackWindow.SetDamage(characterStats.attackDamage);    // 设置攻击判定的伤害值
        
        GameLogger.Log($"[Initialized] currentHealth 设置为: {currentHealth}, maxHealth: {characterStats.maxHealth}", "PlayerController");
        
        // 初始化血条显示
        if (hpBar != null)
        {
            hpBar.SetHP(currentHealth, characterStats.maxHealth);
        }
    }

    /// <summary>
    /// 处理攻击输入
    /// </summary>
    void HandleAttackInput()
    {
        if (isAttacking)
        {
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
        
        // 播放死亡动画
        if (ComponentValidator.CanPlayAnimation(animator, deathAnimation))
        {
            animator.Play(deathAnimation.name);
        }
        
        // 通知Boss停止攻击序列
        if (bossController != null)
        {
            bossController.OnPlayerDeath();
        }
        
        // 可以在这里添加：
        // - 显示游戏结束界面
        // - 重置关卡
        // - 播放游戏结束音效
        
        // 禁用控制
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
    
    /// <summary>
    /// Unity编辑器验证方法（确保退出Play模式后状态重置）
    /// </summary>
    void OnValidate()
    {
        // 确保在编辑器模式下（非运行时）脚本是启用的
        if (!Application.isPlaying && !enabled)
        {
            enabled = true;
        }
    }
}
