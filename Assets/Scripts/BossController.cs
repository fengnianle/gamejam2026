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
    /// 动画绑定
    /// </summary>
    [Header("动画绑定")]
    [Tooltip("待机动画片段")]
    public AnimationClip idleAnimation;
    
    [Tooltip("攻击X动画片段")]
    public AnimationClip attackXAnimation;
    
    [Tooltip("攻击Y动画片段")]
    public AnimationClip attackYAnimation;
    
    [Tooltip("攻击B动画片段")]
    public AnimationClip attackBAnimation;

    /// <summary>
    /// 定力值系统
    /// </summary>
    [Header("定力值系统")]
    [Tooltip("最大定力值")]
    public float maxHealth = 200f;
    
    [Tooltip("当前定力值")]
    public float currentHealth = 200f;
 
    [Tooltip("攻击力")]
    public float attackDamage = 30f;

    /// <summary>
    /// 动作序列系统
    /// </summary>
    [Header("动作序列设置")]
    [Tooltip("Boss的动作序列，按顺序执行")]
    public List<BossAction> actionSequence = new List<BossAction>();
    
    [Tooltip("是否循环播放动作序列")]
    public bool loopSequence = true;
    
    [Tooltip("是否自动开始播放")]
    public bool autoStart = true;
    
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
    void Start()
    {
        // 获取Animator组件
        animator = GetComponent<Animator>();
        attackWindow = GetComponent<AttackWindow>();

        Initialized();

        // 如果动作序列为空，添加默认序列
        if (actionSequence.Count == 0)
        {
            GameLogger.LogWarning("BossController: TODO: 动作序列为空，添加默认序列。", "BossController");
            actionSequence.Add(new BossAction { actionType = BossActionType.AttackX, duration = 1f });
            actionSequence.Add(new BossAction { actionType = BossActionType.Idle, duration = 1f });
            actionSequence.Add(new BossAction { actionType = BossActionType.AttackY, duration = 1f });
            actionSequence.Add(new BossAction { actionType = BossActionType.Idle, duration = 1f });
            actionSequence.Add(new BossAction { actionType = BossActionType.AttackB, duration = 1f });
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
                BossActionType.AttackX => AttackType.Attack1,
                BossActionType.AttackY => AttackType.Attack2,
                BossActionType.AttackB => AttackType.Attack3,
                _ => AttackType.Attack1
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
        GameLogger.LogDamageTaken("Boss", damage, currentHealth, maxHealth);

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

    // ==================== 动作序列控制 ====================
    /// <summary>
    /// 开始播放动作序列
    /// </summary>
    private void StartSequence()
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
    private void StopSequence()
    {
        isPlaying = false;
        isPerformingAction = false;
        CancelInvoke();
        PlayIdleAnimation();
    }

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

        GameLogger.LogBossAction($"执行动作 {currentAction.actionType}，持续时间 {currentAction.duration} 秒");
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
        
        // 可以在这里添加：
        // - 播放死亡动画
        // - 禁用控制
        // - 显示胜利界面
        // - 掉落奖励
        // - 触发下一阶段或结束战斗
        
        // 禁用脚本
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
}

/// <summary> ----------------------------------------- 数据类型 ----------------------------------------- </summary>
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
