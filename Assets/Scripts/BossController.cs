using UnityEngine;
using System.Collections.Generic;

/* 
 * Unity组件绑定说明：
 * 1. 在Hierarchy中选择Boss对象
 * 2. 在Inspector中添加以下组件：
 *    - Animator组件（必需）：用于播放动画
 * 3. 将本脚本挂载到Boss对象上
 * 4. 在本脚本的Inspector面板中：
 *    - 将Idle动画片段拖拽到"Idle Animation"字段
 *    - 将Attack1动画片段拖拽到"Attack1 Animation"字段
 *    - 将Attack2动画片段拖拽到"Attack2 Animation"字段
 *    - 将Attack3动画片段拖拽到"Attack3 Animation"字段
 * 5. 在"动作序列"中添加动作：
 *    - 点击+号添加新动作
 *    - 选择动作类型（Idle/Attack1/Attack2/Attack3）
 *    - 设置每个动作的持续时间
 */

public class BossController : MonoBehaviour
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

        // 如果动作序列为空，添加默认序列
        if (actionSequence.Count == 0)
        {
            Debug.LogWarning("BossController: 动作序列为空，添加默认序列。");
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
            Debug.LogWarning("BossController: 动作序列为空，无法开始播放。");
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

        Debug.Log($"BossController: 执行动作 {currentAction.actionType}，持续时间 {currentAction.duration} 秒");
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
                Debug.Log("BossController: 动作序列播放完成。");
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
