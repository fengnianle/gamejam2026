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
    /// ⚠️ 必须拖拽赋值的场景对象引用
    /// </summary>
    [Space(10)]
    [Header("⚠️ 场景对象引用 - 必须手动拖拽赋值 ⚠️")]
    [Space(5)]
    [Tooltip("⚠️ 必须赋值：玩家血条UI（请在Inspector中拖拽赋值）")]
    public HPBar hpBar;
    
    [Tooltip("⚠️ 必须赋值：Boss控制器（请在Inspector中拖拽赋值）")]
    public BossController bossController;
    
    [Tooltip("⚠️ 必须赋值：玩家路径记录器（用于影子系统，请在Inspector中拖拽赋值）")]
    public PlayerPathRecorder pathRecorder;
    
    [Tooltip("可选：玩家影子控制器（用于影子系统）")]
    public PlayerShadowController shadowController;

    /// <summary>
    /// 动画绑定
    /// </summary>
    [Space(10)]
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
    
    [Header("调试选项")]
    [Tooltip("启用自动反制（用于调试测试，勾选后会自动执行正确的反制动作）")]
    public bool autoCounterEnabled = false;
    
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
    private bool canAcceptInput = false; // 是否可以接受玩家输入（初始为false，等待GameManager开启）
    private bool hasAutoCountered = false; // 标记本次攻击窗口是否已经自动反制过

    /// <summary> ----------------------------------------- 生命周期 ----------------------------------------- </summary>
    void Awake()
    {
        // 在Awake中获取组件，确保最早获取
        animator = GetComponent<Animator>();
        attackWindow = GetComponent<AttackWindow>();
        counterDetector = GetComponent<CounterInputDetector>();
    }

    void Start()
    {
        // 如果 pathRecorder 未手动赋值，自动从单例获取
        if (pathRecorder == null)
        {
            pathRecorder = PlayerPathRecorder.Instance;
            GameLogger.Log("PlayerController: pathRecorder 未手动赋值，自动从单例获取", "PlayerController");
        }
        
        Initialized();

        // 强制初始化为Idle状态（确保游戏开始前处于idle）
        ForcePlayIdle();
    }

    void Update()
    {
        // 自动反制系统（调试用）
        if (autoCounterEnabled)
        {
            HandleAutoCounter();
        }
        
        // 检测处理攻击输入
        HandleAttackInput();
    }

    /// <summary> ----------------------------------------- Public  ----------------------------------------- </summary>
    /// <summary>
    /// 重置玩家状态（由GameManager调用，用于Restart/EndGame）
    /// 注意：不重置canAcceptInput，由GameManager通过SetInputEnabled()统一控制
    /// </summary>
    public void ResetState()
    {
        GameLogger.Log("重置玩家状态", "PlayerController");
        
        // 重新启用组件（死亡时会被禁用）
        enabled = true;
        
        // 恢复animator速度（死亡时会被设为0）
        if (animator != null)
        {
            animator.speed = 1f;
        }
        
        // 取消所有延迟调用
        CancelInvoke();
        
        // 重置状态标记
        isAttacking = false;
        // 注意：不重置canAcceptInput，避免与GameManager的SetInputEnabled()冲突
        hasAutoCountered = false;
        
        // 重置生命值
        if (characterStats != null)
        {
            currentHealth = characterStats.maxHealth;
            
            // 更新血条显示
            if (hpBar != null)
            {
                hpBar.SetHP(currentHealth, characterStats.maxHealth);
            }
        }
        
        // 重置动画状态
        ForcePlayIdle();
        
        // 重置攻击窗口
        if (attackWindow != null)
        {
            attackWindow.EndWindow(); // 确保关闭任何打开的窗口
        }
        
        // 重置反制检测器
        if (counterDetector != null)
        {
            counterDetector.enabled = true; // 确保启用
        }
        
        GameLogger.Log("玩家状态重置完成", "PlayerController");
    }
    
    /// <summary>
    /// 设置是否接受玩家输入（由GameManager调用）
    /// </summary>
    public void SetInputEnabled(bool enabled)
    {
        canAcceptInput = enabled;
        GameLogger.Log($"Player输入已{(enabled ? "启用" : "禁用")}", "PlayerController");
        
        // 如果禁用输入，确保播放Idle动画
        if (!enabled && !isAttacking)
        {
            PlayIdleAnimation();
        }
    }
    
    /// <summary>
    /// 强制播放Idle动画（由GameManager调用，用于初始化状态）
    /// </summary>
    public void ForcePlayIdle()
    {
        // 取消所有待执行的Invoke
        CancelInvoke();
        isAttacking = false;
        // 直接播放idle动画，绕过canAcceptInput检查
        PlayIdleAnimationInternal();
        GameLogger.Log("Player强制进入Idle状态", "PlayerController");
    }
    
    /// <summary>
    /// 公共方法：停止攻击可进行下一次政击（可在动画事件中调用）
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
        if (counterDetector != null && counterDetector.IsInvincible())
        {
            GameLogger.LogInvincibility("Player处于无敌状态，免疫伤害！");
            return;
        }

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth); // 确保不会小于0
        
        // 使用新的战斗过程日志
        GameLogger.LogCombatDamage("Player", damage, currentHealth, characterStats.maxHealth);

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
        // 验证配置
        if (characterStats == null)
        {
            GameLogger.LogError("CharacterStats未赋值！请在Inspector中拖拽赋值", "PlayerController");
            return;
        }
        
        // 从配置中读取初始值
        currentHealth = characterStats.maxHealth;  // 初始化生命值
        attackWindow.SetDamage(characterStats.attackDamage);    // 设置攻击判定的伤害值
        
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
        // 检查是否可以接受输入（死亡后禁用）
        if (!canAcceptInput || isAttacking)
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
    /// 自动反制处理（调试用）
    /// 关键修复：使用FixedUpdate或检测窗口开启时间，确保在窗口超时前触发
    /// </summary>
    void HandleAutoCounter()
    {
        // 【调试功能】不检查canAcceptInput，允许在任何时候自动反制
        // 但仍然检查是否正在攻击，避免打断当前动作
        if (isAttacking)
        {
            return;
        }

        // 检查Boss是否存在攻击窗口
        if (bossController == null) return;
        
        // 获取Boss的AttackWindow组件
        AttackWindow bossAttackWindow = bossController.GetComponent<AttackWindow>();
        if (bossAttackWindow == null) return;
        
        // 检查攻击窗口是否激活
        if (!bossAttackWindow.IsWindowActive())
        {
            // 窗口未激活时，重置标记（为下一次攻击做准备）
            hasAutoCountered = false;
            return;
        }
        
        // 如果本次攻击窗口已经反制过，不再重复反制
        if (hasAutoCountered) return;
        
        // 【关键修复】立即执行反制，不等待下一帧
        // 根据Boss的攻击类型，自动执行对应的反制动作
        AttackType bossAttackType = bossAttackWindow.GetAttackType();
        ExecuteAutoCounterImmediately(bossAttackType, bossAttackWindow);
        
        // 标记已经反制过
        hasAutoCountered = true;
    }
    
    /// <summary>
    /// 立即执行自动反制（修复时序问题）
    /// </summary>
    void ExecuteAutoCounterImmediately(AttackType bossAttackType, AttackWindow bossAttackWindow)
    {
        AnimationClip counterClip = null;
        string counterKey = "";
        AttackType playerAttackType = bossAttackType; // 相同的攻击类型才能反制
        
        // 根据Boss的攻击类型选择对应的反制动作
        switch (bossAttackType)
        {
            case AttackType.AttackX:
                counterClip = attackBAnimation;
                counterKey = "E";
                playerAttackType = AttackType.AttackB;
                break;
            case AttackType.AttackY:
                counterClip = attackXAnimation;
                counterKey = "Q";
                playerAttackType = AttackType.AttackX;
                break;
            case AttackType.AttackB:
                counterClip = attackYAnimation;
                counterKey = "W";
                playerAttackType = AttackType.AttackY;
                break;
        }
        
        if (counterClip != null && ComponentValidator.CanPlayAnimation(animator, counterClip))
        {
            GameLogger.Log($"[自动反制] 检测到Boss的{bossAttackType}攻击，立即执行{counterKey}反制", "AutoCounter");
            
            // 1. 立即通知Boss窗口反制成功（在播放动画之前！）
            string actionName = $"{counterKey}键反制(自动)";
            AttackRelationship.AttackResult result = AttackRelationship.JudgeAttack(bossAttackType, playerAttackType);
            bossAttackWindow.OnPlayerResponse(actionName, result);
            
            GameLogger.Log($"[自动反制] 反制判定完成：{AttackRelationship.GetResultDescription(result)}", "AutoCounter");
            
            // 2. 播放Player的攻击动画
            isAttacking = true;
            animator.Play(counterClip.name);
            
            // 3. 设置Player攻击窗口的攻击类型
            attackWindow.SetAttackType(playerAttackType);
            
            // 4. 启动Player的攻击判定窗口
            attackWindow.StartWindow();
            
            // 5. 通知反制检测器（为了触发无敌状态等效果）
            if (counterDetector != null)
            {
                counterDetector.OnEnemyAttackStart(bossAttackType, bossAttackWindow);
            }
            
            // 6. 延迟关闭Player的攻击窗口和重置状态
            Invoke(nameof(AutoCounterCleanup), counterClip.length);
        }
    }
    
    /// <summary>
    /// 执行自动反制（旧方法，保留以防需要）
    /// </summary>
    void ExecuteAutoCounter(AttackType bossAttackType)
    {
        // 获取Boss的AttackWindow
        AttackWindow bossAttackWindow = bossController.GetComponent<AttackWindow>();
        if (bossAttackWindow != null && bossAttackWindow.IsWindowActive())
        {
            ExecuteAutoCounterImmediately(bossAttackType, bossAttackWindow);
        }
    }
    
    /// <summary>
    /// 自动反制清理（关闭攻击窗口并重置状态）
    /// </summary>
    void AutoCounterCleanup()
    {
        // 关闭Player的攻击窗口
        if (attackWindow != null && attackWindow.IsWindowActive())
        {
            attackWindow.EndWindow();
        }
        
        // 重置攻击状态
        isAttacking = false;
        
        // 返回Idle状态（只要玩家还活着）
        if (canAcceptInput)
        {
            PlayIdleAnimation();
        }
    }
    
    /// <summary>
    /// 重置自动反制标记（已移除，改为在HandleAutoCounter中自动重置）
    /// </summary>
    void ResetAutoCounterFlag()
    {
        hasAutoCountered = false;
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
        
        // 记录玩家输入到路径记录器
        // 如果引用丢失，重新从单例获取
        if (pathRecorder == null)
        {
            pathRecorder = PlayerPathRecorder.Instance;
            if (pathRecorder != null)
            {
                GameLogger.Log("PlayerController.PerformAttack(): pathRecorder 引用丢失，重新从单例获取", "PlayerController");
            }
        }
        
        if (pathRecorder != null)
        {
            AttackType attackType = GetAttackTypeFromClip(attackClip);
            pathRecorder.RecordInput(attackType);
        }
        else
        {
            GameLogger.LogError("PlayerController.PerformAttack(): pathRecorder 为 null，无法记录输入！", "PlayerController");
        }
    }
    
    /// <summary>
    /// 根据动画片段获取政击类型
    /// </summary>
    AttackType GetAttackTypeFromClip(AnimationClip clip)
    {
        if (clip == attackXAnimation) return AttackType.AttackX;
        if (clip == attackYAnimation) return AttackType.AttackY;
        if (clip == attackBAnimation) return AttackType.AttackB;
        return AttackType.AttackX; // 默认值
    }

    /// <summary>
    /// 重置攻击状态并返回Idle
    /// </summary>
    void ResetAttackState()
    {
        // 如果玩家已死亡，不进行任何状态重置
        if (!canAcceptInput)
        {
            return;
        }
        
        isAttacking = false;
        PlayIdleAnimation();
    }

    /// <summary>
    /// 播放Idle动画
    /// </summary>
    void PlayIdleAnimation()
    {
        // 如果玩家已死亡，不播放idle动画
        if (!canAcceptInput)
        {
            return;
        }
        
        PlayIdleAnimationInternal();
    }

    /// <summary>
    /// 内部方法：直接播放Idle动画（绕过canAcceptInput检查）
    /// </summary>
    void PlayIdleAnimationInternal()
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
    /// 冻结死亡动画在最后一帧
    /// </summary>
    void FreezeDeathAnimation()
    {
        if (animator != null)
        {
            // 将animator速度设为0，停留在当前帧（死亡动画的最后一帧）
            animator.speed = 0f;
            GameLogger.Log("Player死亡动画播放完成，停留在最后一帧", "PlayerController");
        }
        
        // 现在可以安全地禁用控制器
        enabled = false;
    }

    /// <summary>
    /// 死亡处理
    /// </summary>
    void Die()
    {
        GameLogger.LogDeath("Player");
        
        // 取消所有待执行的Invoke回调（防止攻击动画结束后的回调触发）
        CancelInvoke();
        
        // 立即禁用玩家输入，防止死亡动画被打断
        canAcceptInput = false;
        isAttacking = true; // 防止任何攻击状态重置
        
        // 禁用反制检测器，停止反制输入检测
        if (counterDetector != null)
        {
            counterDetector.enabled = false;
            GameLogger.Log("Player死亡，反制检测器已禁用", "PlayerController");
        }
        
        // 播放死亡动画
        if (ComponentValidator.CanPlayAnimation(animator, deathAnimation))
        {
            animator.Play(deathAnimation.name);
            
            // 在死亡动画播放完成后，暂停animator以停留在最后一帧
            // 延迟时间设为动画长度，确保动画完整播放
            Invoke(nameof(FreezeDeathAnimation), deathAnimation.length);
        }
        
        // 通知PathRecorder玩家死亡，更新最远路径
        // 如果引用丢失，重新从单例获取
        if (pathRecorder == null)
        {
            pathRecorder = PlayerPathRecorder.Instance;
            GameLogger.Log("PlayerController.Die(): pathRecorder 引用丢失，重新从单例获取", "PlayerController");
        }
        
        if (pathRecorder != null)
        {
            pathRecorder.OnPlayerDeath();
        }
        else
        {
            GameLogger.LogError("PlayerController.Die(): pathRecorder 为 null，无法通知死亡事件！", "PlayerController");
        }
        
        // 通知GameManager玩家死亡
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDeath();
        }
        else
        {
            // 如果没有GameManager，使用旧的方式通知Boss
            if (bossController != null)
            {
                bossController.OnPlayerDeath();
            }
        }
        
        // 可以在这里添加：
        // - 显示游戏结束界面
        // - 重置关卡
        // - 播放游戏结束音效
        
        // 禁用控制（但不禁用整个组件，以便Invoke可以执行）
        // enabled = false; // 注释掉，改为在FreezeDeathAnimation中禁用
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
