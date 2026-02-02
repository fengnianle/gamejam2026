using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Boss控制器
/// 详细的组件绑定说明请查看项目根目录的 README.md 文件
/// </summary>
[RequireComponent(typeof(Animator))]
public class BossController : MonoBehaviour, IDamageable
{
    [Header("动画绑定")]
    [Tooltip("待机动画片段")]
    public AnimationClip idleAnimation;
    
    [Tooltip("攻击1动画片段")]
    public AnimationClip attackXAnimation;
    
    [Tooltip("攻击2动画片段")]
    public AnimationClip attackYAnimation;
    
    [Tooltip("攻击3动画片段")]
    public AnimationClip attackBAnimation;

    [Header("攻击判定设置")]
    [Tooltip("攻击判定窗口对象（通常是子对象上的AttackWindow组件）")]
    public AttackWindow attackWindow;
    
    [Tooltip("攻击伤害值")]
    public float attackDamage = 15f;

    [Header("生命值设置")]
    [Tooltip("最大生命值")]
    public float maxHealth = 200f;
    
    [Tooltip("当前生命值")]
    public float currentHealth = 200f;

    [Header("动作序列设置")]
    [Tooltip("Boss的动作序列，按顺序执行")]
    public List<BossAction> actionSequence = new List<BossAction>();
    
    [Tooltip("是否循环播放动作序列")]
    public bool loopSequence = true;
    
    [Tooltip("是否自动开始播放")]
    public bool autoStart = true;
    
    [Tooltip("动作之间的间隔时间（秒）")]
    public float actionInterval = 1f;

    [Header("组件引用")]
    private Animator animator;

    [Header("状态")]
    private int currentActionIndex = 0;
    private bool isPerformingAction = false;
    private bool isPlaying = false;
    private float actionTimer = 0f;

    void Start()
    {
        // 获取Animator组件
        animator = GetComponent<Animator>();

        // 验证组件和动画绑定
        ComponentValidator.ValidateAnimator(animator, "BossController");
        ComponentValidator.ValidateAnimationClips("BossController", 
            idleAnimation, attackXAnimation, attackYAnimation, attackBAnimation);

        // 验证攻击判定窗口
        if (attackWindow == null)
        {
            GameLogger.LogWarning("BossController: 未绑定AttackWindow组件！攻击将无法造成伤害。", "BossController");
        }
        else
        {
            // 设置攻击判定的伤害值
            attackWindow.SetDamage(attackDamage);
        }

        // 初始化生命值
        currentHealth = maxHealth;

        // 如果动作序列为空，添加默认序列
        if (actionSequence.Count == 0)
        {
            GameLogger.LogWarning("BossController: 动作序列为空，添加默认序列。", "BossController");
            actionSequence.Add(new BossAction { actionType = BossActionType.Attack1, duration = 1f });
            actionSequence.Add(new BossAction { actionType = BossActionType.Idle, duration = 1f });
            actionSequence.Add(new BossAction { actionType = BossActionType.Attack2, duration = 1f });
            actionSequence.Add(new BossAction { actionType = BossActionType.Idle, duration = 1f });
            actionSequence.Add(new BossAction { actionType = BossActionType.Attack3, duration = 1f });
            actionSequence.Add(new BossAction { actionType = BossActionType.Idle, duration = 2f });
        }

        // 自动开始
        if (autoStart)
        {
            StartSequence();
        }
    }

    void Update()
    {
        if (!isPlaying) return;

        // 如果正在执行动作，等待动作完成
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

    /// <summary>
    /// 开始播放动作序列
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
        ExecuteCurrentAction();
    }

    /// <summary>
    /// 停止播放动作序列
    /// </summary>
    public void StopSequence()
    {
        isPlaying = false;
        isPerformingAction = false;
        CancelInvoke();
        PlayIdleAnimation();
    }

    /// <summary>
    /// 暂停播放动作序列
    /// </summary>
    public void PauseSequence()
    {
        isPlaying = false;
    }

    /// <summary>
    /// 继续播放动作序列
    /// </summary>
    public void ResumeSequence()
    {
        isPlaying = true;
    }

    /// <summary>
    /// 重置动作序列到开始
    /// </summary>
    public void ResetSequence()
    {
        StopSequence();
        currentActionIndex = 0;
    }

    /// <summary>
    /// 执行当前动作
    /// </summary>
    void ExecuteCurrentAction()
    {
        if (!isPlaying || actionSequence.Count == 0) return;

        BossAction currentAction = actionSequence[currentActionIndex];
        isPerformingAction = true;
        actionTimer = currentAction.duration;

        // 播放对应的动画
        PlayActionAnimation(currentAction.actionType);

        GameLogger.LogBossAction($"执行动作 {currentAction.actionType}，持续时间 {currentAction.duration} 秒");
    }

    /// <summary>
    /// 执行下一个动作
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
    /// 播放动作动画
    /// </summary>
    void PlayActionAnimation(BossActionType actionType)
    {
        if (animator == null) return;

        AnimationClip clipToPlay = actionType switch
        {
            BossActionType.Idle => idleAnimation,
            BossActionType.Attack1 => attackXAnimation,
            BossActionType.Attack2 => attackYAnimation,
            BossActionType.Attack3 => attackBAnimation,
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
    /// 添加动作到序列
    /// </summary>
    public void AddAction(BossActionType actionType, float duration)
    {
        actionSequence.Add(new BossAction { actionType = actionType, duration = duration });
    }

    /// <summary>
    /// 清空动作序列
    /// </summary>
    public void ClearSequence()
    {
        actionSequence.Clear();
    }

    // ==================== 攻击判定窗口控制 ====================
    
    /// <summary>
    /// 开启攻击判定窗口（由Animation Event调用）
    /// 在攻击动画的合适帧添加此Event
    /// </summary>
    public void OnAttackHitboxStart()
    {
        GameLogger.LogAnimationEvent("Boss", "OnAttackHitboxStart");
        
        if (attackWindow != null)
        {
            // 根据当前动作类型设置攻击类型
            if (actionSequence.Count > 0 && currentActionIndex < actionSequence.Count)
            {
                BossActionType currentAction = actionSequence[currentActionIndex].actionType;
                AttackType attackType = currentAction switch
                {
                    BossActionType.Attack1 => AttackType.Attack1,
                    BossActionType.Attack2 => AttackType.Attack2,
                    BossActionType.Attack3 => AttackType.Attack3,
                    _ => AttackType.Attack1
                };
                attackWindow.SetAttackType(attackType);
            }
            
            attackWindow.StartWindow();
        }
        else
        {
            GameLogger.LogWarning("BossController: AttackWindow未绑定，无法启用攻击判定！", "BossController");
        }
    }

    /// <summary>
    /// 关闭攻击判定窗口（由Animation Event调用）
    /// 在攻击动画结束前的合适帧添加此Event
    /// </summary>
    public void OnAttackHitboxEnd()
    {
        GameLogger.LogAnimationEvent("Boss", "OnAttackHitboxEnd");
        
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
        currentHealth -= damage;
        GameLogger.LogDamageTaken("Boss", damage, currentHealth, maxHealth);

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
        // - 进入下一阶段（如果Boss有多阶段）
    }

    /// <summary>
    /// 死亡处理
    /// </summary>
    void Die()
    {
        GameLogger.LogDeath("Boss");
        
        // 停止动作序列
        StopSequence();
        
        // 可以在这里添加：
        // - 播放死亡动画
        // - 禁用控制
        // - 显示胜利界面
        // - 掉落奖励
        // - 触发下一阶段或结束战斗
        
        // 暂时禁用脚本
        enabled = false;
    }

    /// <summary>
    /// 恢复生命值
    /// </summary>
    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        GameLogger.LogHeal("Boss", amount, currentHealth, maxHealth);
    }
}

/// <summary>
/// Boss动作类型
/// </summary>
public enum BossActionType
{
    Idle,       // 待机
    Attack1,    // 攻击1
    Attack2,    // 攻击2
    Attack3     // 攻击3
}

/// <summary>
/// Boss动作数据
/// </summary>
[System.Serializable]
public class BossAction
{
    [Tooltip("动作类型")]
    public BossActionType actionType;
    
    [Tooltip("动作持续时间（秒）")]
    public float duration = 1f;
}
