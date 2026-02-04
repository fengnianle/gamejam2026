using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Boss控制器
/// 详细的组件绑定说明请查看项目根目录的 README.md 文件
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AttackWindow))]
public class BossController : MonoBehaviour
{
    /// <summary>
    /// ⚠️ 必须拖拽赋值的场景对象引用
    /// </summary>
    [Space(10)]
    [Header("⚠️ 场景对象引用 - 必须手动拖拽赋值 ⚠️")]
    [Space(5)]
    [Tooltip("⚠️ 必须赋值：Boss血条UI（请在Inspector中拖拽赋值）")]
    public HPBar hpBar;
    
    [Tooltip("可选：Boss的影子控制器（用于预判系统）")]
    public BossShadowController shadowController;

    /// <summary>
    /// 动画绑定
    /// </summary>
    [Space(10)]
    [Header("动画绑定")]
    [Tooltip("待机动画片段")]
    public AnimationClip idleAnimation;
    
    [Tooltip("攻击X动画片段")]
    public AnimationClip attackXAnimation;
    
    [Tooltip("攻击Y动画片段")]
    public AnimationClip attackYAnimation;
    
    [Tooltip("攻击B动画片段")]
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
    [System.NonSerialized]
    public float currentHealth;

    /// <summary>
    /// 动作序列系统
    /// </summary>
    [Header("动作序列设置")]
    [Tooltip("Boss的攻击模式配置（X=AttackX, Y=AttackY, B=AttackB）")]
    public List<BossAttackPattern> attackPatterns = new List<BossAttackPattern>();
    
    [Tooltip("每个攻击动作的持续时间（秒）")]
    public float attackDuration = 1f;
    
    [Tooltip("Boss的动作序列（自动生成，无需手动配置）")]
    [HideInInspector]
    public List<BossAction> actionSequence = new List<BossAction>();
    
    [Tooltip("是否循环播放动作序列")]
    public bool loopSequence = true;
    
    [Tooltip("动作之间的间隔时间（秒）")]
    public float actionInterval = 1f;

    /// <summary>
    /// 组件获取
    /// </summary>
    [Header("组件引用")]
    [Tooltip("动画控制器（自动获取）")]
    private Animator animator;
    [Tooltip("攻击判定窗口对象（自动获取）")]
    private AttackWindow attackWindow;

    /// <summary>
    /// 动作执行状态
    /// </summary>
    [Header("状态")]
    private int currentActionIndex = 0;
    [Tooltip("是否当前正在执行动作")]
    private bool isPerformingAction = false;
     [Tooltip("是否正在播放动作序列")]
    private bool isPlaying = false;
    [Tooltip("当前动作执行的倒计时")]
    private float actionTimer = 0f;

    /// <summary> ----------------------------------------- 生命周期 ----------------------------------------- </summary>
    void Awake()
    {
        // 获取组件引用（在Awake中确保最早获取）
        animator = GetComponent<Animator>();
        attackWindow = GetComponent<AttackWindow>();
        
        // 验证关键组件
        if (animator == null)
        {
            GameLogger.LogError("[Awake] Animator组件获取失败！请确保GameObject上挂载了Animator组件", "Boss");
        }
        else
        {
            GameLogger.Log("[Awake] Animator组件获取成功", "Boss");
        }
    }

    void Start()
    {
        // 验证拖拽赋值的组件
        if (hpBar == null)
        {
            GameLogger.LogWarning("[Start] hpBar未赋值，请在Inspector中拖拽赋值", "Boss");
        }
        else
        {
            GameLogger.Log($"[Start] hpBar已赋值: {hpBar.gameObject.name}", "Boss");
        }
        
        Initialized();

        // 从attackPatterns生成actionSequence
        GenerateActionSequence();

        // 如果动作序列仍然为空，添加默认序列
        if (actionSequence.Count == 0)
        {
            GameLogger.LogWarning("BossController: 动作序列为空，添加默认序列。", "BossController");
            actionSequence.Add(new BossAction { actionType = BossActionType.AttackX, duration = 1f });
            actionSequence.Add(new BossAction { actionType = BossActionType.Idle, duration = 1f });
            actionSequence.Add(new BossAction { actionType = BossActionType.AttackX, duration = 1f });
            actionSequence.Add(new BossAction { actionType = BossActionType.Idle, duration = 1f });
            actionSequence.Add(new BossAction { actionType = BossActionType.AttackX, duration = 1f });
            actionSequence.Add(new BossAction { actionType = BossActionType.Idle, duration = 2f });
        }

        // Boss的启动完全由GameManager控制
        // 初始状态：播放Idle动画，等待GameManager调用StartSequence()
        ForcePlayIdle();
        GameLogger.Log("Boss初始化完成，等待GameManager启动序列", "Boss");
    }

    void Update()
    {
        if (!isPlaying) return;

        // 等待当前动作完成
        if (isPerformingAction)
        {
            actionTimer -= Time.deltaTime;
            if (actionTimer <= 0f)
            {
                isPerformingAction = false;
                
                // 等待间隔时间后执行下一个动作
                Invoke(nameof(ExecuteNextAction), actionInterval);
            }
        }
    }

    /// <summary> ----------------------------------------- Public ----------------------------------------- </summary>
    /// <summary>
    /// 重置Boss状态（由GameManager调用，用于Restart/EndGame）
    /// </summary>
    public void ResetState()
    {
        GameLogger.Log("重置Boss状态", "BossController");
        
        // 重新启用组件（死亡时会被禁用）
        enabled = true;
        
        // 恢复animator速度（如果死亡时被修改）
        if (animator != null)
        {
            animator.speed = 1f;
        }
        
        // 取消所有延迟调用
        CancelInvoke();
        
        // 重置状态标记
        isPlaying = false;
        isPerformingAction = false;
        currentActionIndex = 0;
        actionTimer = 0f;
        
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
        
        // 重置影子控制器
        if (shadowController != null)
        {
            shadowController.ResetState();
        }
        
        GameLogger.Log("Boss状态重置完成", "BossController");
    }
    
    /// <summary>
    /// 添加新动作到序列末尾
    /// </summary>
    // public void AddAction(BossActionType actionType, float duration)
    // {
    //     actionSequence.Add(new BossAction { actionType = actionType, duration = duration });
    // }

    /// <summary>
    /// 清空所有动作序列
    /// </summary>
    // public void ClearSequence()
    // {
    //     actionSequence.Clear();
    // }

    // ==================== 攻击判定窗口控制 ====================
    /// <summary>
    /// 开启攻击判定窗口（由Animation Event调用）
    /// </summary>
    public void OnAttackWindowStart()
    {
        GameLogger.LogAttackWindow("Boss OnAttackWindow Start");

        // 根据当前动作类型设置攻击类型
        if (actionSequence.Count > 0 && currentActionIndex < actionSequence.Count)
        {
            BossActionType currentAction = actionSequence[currentActionIndex].actionType;
            AttackType attackType = currentAction switch
            {
                BossActionType.AttackX => AttackType.AttackX,
                BossActionType.AttackY => AttackType.AttackY,
                BossActionType.AttackB => AttackType.AttackB,
                _ => AttackType.AttackX
            };
            attackWindow.SetAttackType(attackType);
        }
        
        attackWindow.StartWindow();
    }

    /// <summary>
    /// 关闭攻击判定窗口（由Animation Event调用）
    /// </summary>
    public void OnAttackWindowEnd()
    {
        GameLogger.LogAttackWindow("Boss OnAttackWindow End");
        
        attackWindow.EndWindow();
    }

    // ==================== 受伤系统 ====================
    /// <summary>
    /// 接收伤害
    /// </summary>
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth); // 确保不会小于0
        
        // 使用新的战斗过程日志
        GameLogger.LogCombatDamage("Boss", damage, currentHealth, characterStats.maxHealth);

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

    /// <summary>
    /// 玩家死亡时调用（停止Boss的政击序列）
    /// </summary>
    public void OnPlayerDeath()
    {
        GameLogger.LogBossAction("玩家已死亡，停止攻击序列");
        StopSequence();
        
        // 播放胜利动画或待机动画
        PlayIdleAnimation();
    }

    // ==================== 动作序列控制 ====================
    /// <summary>
    /// 开始播放动作序列（由GameManager调用）
    /// </summary>
    public void StartSequence()
    {
        if (actionSequence.Count == 0)
        {
            GameLogger.LogWarning("BossController: 动作序列为空，无法开始播放。", "BossController");
            return;
        }

        isPlaying = true;
        currentActionIndex = 0;
        
        // 先启动影子（如果存在），让影子提前播放
        if (shadowController != null)
        {
            // 延迟启动影子，让它提前leadTime秒开始
            Invoke(nameof(StartShadowWithDelay), 0f);
        }
        
        // Boss在影子启动后leadTime秒再启动
        if (shadowController != null)
        {
            Invoke(nameof(StartBossSequence), shadowController.leadTime);
        }
        else
        {
            // 如果没有影子，立即启动Boss
            StartBossSequence();
        }
        
        GameLogger.LogBossAction("开始攻击序列（包含影子系统）");
    }
    
    /// <summary>
    /// 启动影子序列（内部方法）
    /// </summary>
    void StartShadowWithDelay()
    {
        if (shadowController != null)
        {
            shadowController.StartShadowSequence();
            GameLogger.Log("影子序列已启动", "BossController");
        }
    }
    
    /// <summary>
    /// 启动Boss自身的序列（内部方法）
    /// </summary>
    void StartBossSequence()
    {
        ExecuteCurrentAction();
        GameLogger.Log("Boss序列已启动", "BossController");
    }

    /// <summary>
    /// 停止播放动作序列（由GameManager调用）
    /// </summary>
    public void StopSequence()
    {
        isPlaying = false;
        isPerformingAction = false;
        CancelInvoke();
        PlayIdleAnimation();
        
        // 同时停止影子
        if (shadowController != null)
        {
            shadowController.StopShadowSequence();
        }
        
        GameLogger.LogBossAction("停止攻击序列");
    }
    
    /// <summary>
    /// 强制播放Idle动画（由GameManager调用，用于初始化状态）
    /// </summary>
    public void ForcePlayIdle()
    {
        // 取消所有待执行的Invoke
        CancelInvoke();
        isPlaying = false;
        isPerformingAction = false;
        PlayIdleAnimation();
        
        // 同时强制影子进入Idle
        if (shadowController != null)
        {
            shadowController.ForcePlayIdle();
        }
        
        GameLogger.LogBossAction("Boss强制进入Idle状态");
    }

    /// <summary> ----------------------------------------- Private ----------------------------------------- </summary>
    void Initialized()
    {
        // 验证配置
        if (characterStats == null)
        {
            GameLogger.LogError("CharacterStats未赋值！请在Inspector中拖拽赋值", "BossController");
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
    /// 从attackPatterns生成actionSequence
    /// </summary>
    void GenerateActionSequence()
    {
        actionSequence.Clear();
        
        if (attackPatterns == null || attackPatterns.Count == 0)
        {
            GameLogger.LogWarning("BossController: attackPatterns为空，无法生成动作序列。", "BossController");
            return;
        }

        foreach (var pattern in attackPatterns)
        {
            if (string.IsNullOrEmpty(pattern.attackSequence))
            {
                GameLogger.LogWarning("BossController: 发现空的攻击序列，跳过。", "BossController");
                continue;
            }

            // 解析攻击序列字符串
            string upperSequence = pattern.attackSequence.ToUpper();
            for (int i = 0; i < upperSequence.Length; i++)
            {
                char c = upperSequence[i];
                
                // 跳过空格等空白字符
                if (char.IsWhiteSpace(c))
                {
                    continue;
                }
                
                BossActionType actionType = c switch
                {
                    'X' => BossActionType.AttackX,
                    'Y' => BossActionType.AttackY,
                    'B' => BossActionType.AttackB,
                    _ => BossActionType.Idle
                };

                // 跳过不识别的字符
                if (c != 'X' && c != 'Y' && c != 'B')
                {
                    GameLogger.LogWarning($"BossController: 不识别的攻击字符 '{c}'，跳过。", "BossController");
                    continue;
                }

                // 添加攻击动作
                actionSequence.Add(new BossAction
                {
                    actionType = actionType,
                    duration = attackDuration
                });
                
                // 如果勾选了攻击之间有间隔，且不是最后一个攻击，则添加短暂的间隔
                if (pattern.hasIntervalBetweenAttacks && i < upperSequence.Length - 1)
                {
                    // 检查下一个字符是否也是有效的攻击字符
                    bool hasNextAttack = false;
                    for (int j = i + 1; j < upperSequence.Length; j++)
                    {
                        if (upperSequence[j] == 'X' || upperSequence[j] == 'Y' || upperSequence[j] == 'B')
                        {
                            hasNextAttack = true;
                            break;
                        }
                    }
                    
                    if (hasNextAttack)
                    {
                        actionSequence.Add(new BossAction
                        {
                            actionType = BossActionType.Idle,
                            duration = pattern.intervalBetweenAttacks
                        });
                    }
                }
            }

            // 在每个pattern结束后添加Idle间隔（作为模式之间的间隔）
            if (pattern.idleTime > 0)
            {
                actionSequence.Add(new BossAction
                {
                    actionType = BossActionType.Idle,
                    duration = pattern.idleTime
                });
            }
        }

        GameLogger.Log($"BossController: 成功生成动作序列，共 {actionSequence.Count} 个动作。", "BossController");
    }

    // ==================== 动作序列控制 ====================

    /// <summary>
    /// 暂停播放动作序列
    /// </summary>
    private void PauseSequence()
    {
        isPlaying = false;
    }

    /// <summary>
    /// 继续播放动作序列
    /// </summary>
    private void ResumeSequence()
    {
        isPlaying = true;
    }

    /// <summary>
    /// 重置动作序列到开始
    /// </summary>
    private void ResetSequence()
    {
        StopSequence();
        currentActionIndex = 0;
    }

    /// <summary>
    /// 执行当前序列中的动作
    /// </summary>
    void ExecuteCurrentAction()
    {
        if (!isPlaying || actionSequence.Count == 0) return;

        BossAction currentAction = actionSequence[currentActionIndex];
        isPerformingAction = true;
        actionTimer = currentAction.duration;

        // 播放对应的动画
        PlayActionAnimation(currentAction.actionType);

        // 使用新的战斗过程日志
        GameLogger.LogCombatBossAction(currentAction.actionType, currentAction.duration);
    }

    /// <summary>
    /// 移动到下一个动作，检查是否循环
    /// </summary>
    void ExecuteNextAction()
    {
        if (!isPlaying) return;

        currentActionIndex++;

        // 检查是否到达序列末尾
        if (currentActionIndex >= actionSequence.Count)
        {
            if (loopSequence)
            {
                currentActionIndex = 0;
                ExecuteCurrentAction();
            }
            else
            {
                GameLogger.LogBossAction("动作序列播放完成。");
                StopSequence();
            }
        }
        else
        {
            ExecuteCurrentAction();
        }
    }

    /// <summary>
    /// 根据动作类型播放对应的动画
    /// </summary>
    void PlayActionAnimation(BossActionType actionType)
    {
        if (animator == null) return;

        AnimationClip clipToPlay = actionType switch
        {
            BossActionType.Idle => idleAnimation,
            BossActionType.AttackX => attackXAnimation,
            BossActionType.AttackY => attackYAnimation,
            BossActionType.AttackB => attackBAnimation,
            _ => idleAnimation
        };

        if (ComponentValidator.ValidateAndLogClip(clipToPlay, actionType.ToString(), "BossController"))
        {
            animator.Play(clipToPlay.name);
        }
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
    /// 受伤时的响应处理
    /// </summary>
    void OnDamaged()
    {
        // 可以在这里添加：
        // - 播放受伤动画
        // - 播放受伤音效
        // - 显示受伤特效
    }

    /// <summary>
    /// 死亡处理
    /// </summary>
    void Die()
    {
        GameLogger.LogDeath("Boss");
        
        // 停止所有动作序列
        StopSequence();
        
        // 播放死亡动画
        if (ComponentValidator.CanPlayAnimation(animator, deathAnimation))
        {
            animator.Play(deathAnimation.name);
            GameLogger.Log("Boss死亡动画开始播放", "BossController");
            
            // 在死亡动画播放完成后禁用脚本
            Invoke(nameof(DisableBossController), deathAnimation.length);
        }
        else
        {
            // 如果没有死亡动画，立即禁用
            DisableBossController();
        }
        
        // 通知GameManager Boss死亡
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnBossDeath();
        }
        
        // 可以在这里添加：
        // - 显示胜利界面
        // - 掉落奖励
        // - 触发下一阶段或结束战斗
        // - 播放胜利音效
    }
    
    /// <summary>
    /// 禁用Boss控制器（死亡动画播放完成后调用）
    /// </summary>
    void DisableBossController()
    {
        GameLogger.Log("Boss死亡动画播放完成，禁用控制器", "BossController");
        enabled = false;
    }

    /// <summary>
    /// 恢复生命值
    /// </summary>
    // public void Heal(float amount)
    // {
    //     currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    //     GameLogger.LogHeal("Boss", amount, currentHealth, maxHealth);
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

/// <summary> ----------------------------------------- 数据类型 ----------------------------------------- </summary>
/// <summary>
/// Boss攻击模式配置
/// 用于简洁地配置Boss的攻击序列
/// 例如：attackSequence = "XXX" 表示连续3个AttackX
/// </summary>
[System.Serializable]
public class BossAttackPattern
{
    [Tooltip("攻击序列字符串（X=AttackX, Y=AttackY, B=AttackB），例如：XXX表示3个X攻击")]
    public string attackSequence = "XXX";
    
    [Tooltip("攻击动作之间是否有间隔（勾选后每个攻击之间会有间隔）")]
    public bool hasIntervalBetweenAttacks = false;
    
    [ConditionalHide("hasIntervalBetweenAttacks")]
    [Tooltip("攻击动作之间的间隔时间（秒），仅当勾选了'攻击动作之间是否有间隔'时显示")]
    public float intervalBetweenAttacks = 0.5f;
    
    [Tooltip("此攻击模式结束后的Idle时间（秒），作为模式之间的间隔")]
    public float idleTime = 2f;
}

/// <summary>
/// Boss动作类型枚举
/// </summary>
public enum BossActionType
{
    Idle,       // 待机
    AttackX,    // 攻击X
    AttackY,    // 攻击Y
    AttackB     // 攻击B
}

/// <summary>
/// Boss动作数据结构
/// </summary>
[System.Serializable]
public class BossAction
{
    [Tooltip("动作类型")]
    public BossActionType actionType;
    
    [Tooltip("动作持续时间（秒）")]
    public float duration = 1f;
}
