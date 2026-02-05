using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Boss影子控制器
/// 作用：提前Boss指定时间（默认1秒）播放相同的动作序列，用于给玩家提供预判信息
/// 
/// 功能特点：
/// - 只播放动画，不触发任何战斗逻辑（无伤害、无血量、无攻击判定）
/// - 自动跟随Boss的动作序列
/// - 可配置提前播放的时间
/// - 支持动作序列的启动、停止、重置
/// 
/// 使用方法：
/// 1. 在场景中创建Boss的影子GameObject（复制Boss的模型和Animator）
/// 2. 移除影子上的AttackWindow、HPBar等战斗组件
/// 3. 添加本脚本到影子GameObject
/// 4. 在Inspector中拖拽赋值Boss引用和动画clips
/// 5. 设置提前播放的延迟时间（默认1秒）
/// </summary>
[RequireComponent(typeof(Animator))]
public class BossShadowController : MonoBehaviour
{
    /// <summary>
    /// ! 必须拖拽赋值的场景对象引用
    /// </summary>
    [Space(10)]
    [Header("! 场景对象引用 - 必须手动拖拽赋值 !")]
    [Space(5)]
    [Tooltip("! 必须赋值：跟随的Boss控制器（请在Inspector中拖拽赋值）")]
    public BossController bossController;

    /// <summary>
    /// 动画绑定（需要与Boss的动画保持一致）
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

    /// <summary>
    /// 影子配置
    /// </summary>
    [Header("影子配置")]
    
    [Tooltip("影子提前播放的时间（秒）。1表示影子比Boss提前1秒执行动作")]
    public float leadTime = 1f;
    
    [Tooltip("是否自动跟随Boss开始（建议保持true）")]
    public bool autoFollow = true;

    /// <summary>
    /// 组件引用
    /// </summary>
    [Header("组件引用")]
    [Tooltip("动画控制器（自动获取）")]
    private Animator animator;

    /// <summary>
    /// 影子状态
    /// </summary>
    [Header("状态")]
    [Tooltip("影子是否正在播放动作序列")]
    private bool isPlaying = false;
    
    [Tooltip("当前动作索引")]
    private int currentActionIndex = 0;
    
    [Tooltip("影子的动作序列（从Boss复制）")]
    private List<BossAction> shadowActionSequence = new List<BossAction>();
    
    [Tooltip("当前动作计时器")]
    private float actionTimer = 0f;
    
    [Tooltip("是否正在执行动作")]
    private bool isPerformingAction = false;

    /// <summary> ----------------------------------------- 生命周期 ----------------------------------------- </summary>
    void Awake()
    {
        // 获取Animator组件
        animator = GetComponent<Animator>();
        
        if (animator == null)
        {
            GameLogger.LogError("[Awake] BossShadow: Animator组件获取失败！", "BossShadow");
        }
    }

    void Start()
    {
        // 验证Boss引用
        if (bossController == null)
        {
            GameLogger.LogError("[Start] BossShadow: BossController未赋值！请在Inspector中拖拽赋值", "BossShadow");
            return;
        }
        
        // 初始化为Idle状态
        PlayIdleAnimation();
        
        GameLogger.Log($"[Start] BossShadow初始化完成，提前时间: {leadTime}秒", "BossShadow");
    }

    void Update()
    {
        if (!isPlaying) return;

        // 更新动作计时
        if (isPerformingAction)
        {
            actionTimer -= Time.deltaTime;
            if (actionTimer <= 0f)
            {
                isPerformingAction = false;
                
                // 获取Boss的动作间隔时间
                float actionInterval = bossController != null ? bossController.actionInterval : 1f;
                
                // 等待间隔时间后执行下一个动作
                Invoke(nameof(ExecuteNextAction), actionInterval);
            }
        }
    }

    /// <summary> ----------------------------------------- Public ----------------------------------------- </summary>
    /// <summary>
    /// 重置影子状态（由BossController调用）
    /// 注意：不清空shadowActionSequence，保留序列数据
    /// </summary>
    public void ResetState()
    {
        GameLogger.Log("重置Boss影子状态", "BossShadow");
        
        // 停止当前播放
        StopShadowSequence();
        
        // 不清空序列，保留数据（StartShadowSequence会重新复制）
        // shadowActionSequence.Clear();
        
        // 重置状态
        currentActionIndex = 0;
        isPlaying = false;
        isPerformingAction = false;
        actionTimer = 0f;
        
        // 播放Idle
        ForcePlayIdle();
        
        GameLogger.Log("Boss影子状态重置完成", "BossShadow");
    }
    
    /// <summary>
    /// 开始播放影子动作序列（提前Boss leadTime秒）
    /// </summary>
    public void StartShadowSequence()
    {
        if (bossController == null)
        {
            GameLogger.LogError("BossShadow: 无法启动，BossController未赋值！", "BossShadow");
            return;
        }

        // 复制Boss的动作序列
        CopyShadowSequenceFromBoss();

        if (shadowActionSequence.Count == 0)
        {
            GameLogger.LogWarning("BossShadow: 动作序列为空，无法开始播放", "BossShadow");
            return;
        }

        // 立即开始播放（提前Boss leadTime秒）
        isPlaying = true;
        currentActionIndex = 0;
        ExecuteCurrentAction();
        
        GameLogger.Log($"BossShadow: 开始播放动作序列（提前{leadTime}秒）", "BossShadow");
    }

    /// <summary>
    /// 停止播放影子动作序列
    /// </summary>
    public void StopShadowSequence()
    {
        isPlaying = false;
        isPerformingAction = false;
        CancelInvoke();
        PlayIdleAnimation();
        
        GameLogger.Log("BossShadow: 停止播放动作序列", "BossShadow");
    }

    /// <summary>
    /// 强制播放Idle动画
    /// </summary>
    public void ForcePlayIdle()
    {
        CancelInvoke();
        isPlaying = false;
        isPerformingAction = false;
        PlayIdleAnimation();
        GameLogger.Log("BossShadow: 强制进入Idle状态", "BossShadow");
    }

    /// <summary> ----------------------------------------- Private ----------------------------------------- </summary>
    /// <summary>
    /// 确保Animator组件存在（保护方法）
    /// </summary>
    void EnsureAnimator()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                GameLogger.LogError("BossShadow: 无法获取Animator组件！", "BossShadow");
            }
            else
            {
                GameLogger.Log("BossShadow: Animator组件引用丢失，已重新获取", "BossShadow");
            }
        }
    }
    
    /// <summary>
    /// 从Boss控制器复制动作序列
    /// 只复制玩家看到过的动作（根据 maxSeenActionIndex）
    /// </summary>
    void CopyShadowSequenceFromBoss()
    {
        shadowActionSequence.Clear();
        
        if (bossController == null || bossController.actionSequence == null)
        {
            return;
        }

        // 获取玩家看到过的最远索引
        int maxSeenIndex = bossController.GetMaxSeenActionIndex();
        
        // 如果玩家还未看到任何政击（maxSeenIndex == -1），则不复制任何动作
        if (maxSeenIndex < 0)
        {
            GameLogger.LogBossAttackPath("玩家还未看到任何Boss攻击，影子不播放任何动作");
            return;
        }
        
        // 只复制到 maxSeenIndex 的动作（包含 maxSeenIndex）
        int copyCount = Mathf.Min(maxSeenIndex + 1, bossController.actionSequence.Count);
        
        for (int i = 0; i < copyCount; i++)
        {
            var action = bossController.actionSequence[i];
            shadowActionSequence.Add(new BossAction
            {
                actionType = action.actionType,
                duration = action.duration
            });
        }
        
        GameLogger.LogBossAttackPath($"影子已复制Boss动作序列，共{shadowActionSequence.Count}个动作（最远索引: {maxSeenIndex + 1}）");
    }

    /// <summary>
    /// 执行当前动作
    /// </summary>
    void ExecuteCurrentAction()
    {
        if (!isPlaying || shadowActionSequence.Count == 0) return;

        BossAction currentAction = shadowActionSequence[currentActionIndex];
        isPerformingAction = true;
        actionTimer = currentAction.duration;

        // 播放对应的动画
        PlayActionAnimation(currentAction.actionType);

        GameLogger.Log($"BossShadow: 执行动作 {currentAction.actionType}，持续时间 {currentAction.duration}秒", "BossShadow");
    }

    /// <summary>
    /// 执行下一个动作
    /// </summary>
    void ExecuteNextAction()
    {
        if (!isPlaying) return;

        currentActionIndex++;

        // 检查是否到达序列末尾
        if (currentActionIndex >= shadowActionSequence.Count)
        {
            // 影子系统不循环播放，播放完所有看到过的动作后停留在Idle
            GameLogger.Log("BossShadow: 动作序列播放完成，停留在Idle状态", "BossShadow");
            StopShadowSequence();
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
        EnsureAnimator(); // 确保animator存在
        if (animator == null) return;

        AnimationClip clipToPlay = actionType switch
        {
            BossActionType.Idle => idleAnimation,
            BossActionType.AttackX => attackXAnimation,
            BossActionType.AttackY => attackYAnimation,
            BossActionType.AttackB => attackBAnimation,
            _ => idleAnimation
        };

        if (ComponentValidator.ValidateAndLogClip(clipToPlay, actionType.ToString(), "BossShadow"))
        {
            // 【关键】强制从头开始播放动画，即使是相同的动画状态
            // 参数: (stateName, layer, normalizedTime)
            // -1 表示默认层，0f 表示从动画的0%位置开始播放
            animator.Play(clipToPlay.name, -1, 0f);
        }
    }

    /// <summary>
    /// 播放Idle动画
    /// </summary>
    void PlayIdleAnimation()
    {
        EnsureAnimator(); // 确保animator存在
        if (ComponentValidator.CanPlayAnimation(animator, idleAnimation))
        {
            animator.Play(idleAnimation.name);
        }
    }
}
