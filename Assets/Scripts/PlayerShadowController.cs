using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 玩家影子控制器
/// 作用：提前指定时间（默认1秒）播放玩家的"最远进度路径"，展示玩家历史最佳操作序列
/// 
/// 功能特点：
/// - 只播放动画，不触发任何战斗逻辑（无伤害、无血量、无攻击判定）
/// - 从PlayerPathRecorder读取最远路径并播放
/// - 首次游戏或EndGame后路径为空时保持Idle
/// - 提前leadTime秒播放，与Boss影子同步
/// - 支持动作序列的启动、停止、重置
/// 
/// 使用方法：
/// 1. 在场景中创建Player的影子GameObject（复制Player的模型和Animator）
/// 2. 移除影子上的AttackWindow、HPBar、CounterInputDetector等战斗组件
/// 3. 添加本脚本到影子GameObject
/// 4. 在Inspector中拖拽赋值PathRecorder引用和动画clips
/// 5. 设置提前播放的延迟时间（默认1秒，与Boss影子相同）
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerShadowController : MonoBehaviour
{
    /// <summary>
    /// 动画绑定（需要与Player的动画保持一致）
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
    /// 影子配置
    /// </summary>
    [Header("影子配置")]
    [Tooltip("玩家路径记录器（必须赋值）")]
    public PlayerPathRecorder pathRecorder;
    
    [Tooltip("影子提前播放的时间（秒）。1表示影子比实际游戏提前1秒执行动作")]
    public float leadTime = 1f;
    
    [Tooltip("每个动作的默认持续时间（秒）")]
    public float actionDuration = 1f;
    
    [Tooltip("动作之间的间隔时间（秒）")]
    public float actionInterval = 1f;

    /// <summary>
    /// 组件引用
    /// </summary>
    [Header("组件引用")]
    [Tooltip("动画控制器（自动获取）")]
    private Animator animator;

    /// <summary>
    /// 运行时状态
    /// </summary>
    [Header("运行时状态")]
    [Tooltip("影子的动作序列（从PathRecorder复制）")]
    [SerializeField] private List<PlayerAction> shadowSequence = new List<PlayerAction>();
    
    [Tooltip("当前执行的动作索引")]
    [SerializeField] private int currentActionIndex = 0;
    
    [Tooltip("是否正在播放")]
    [SerializeField] private bool isPlaying = false;
    
    [Tooltip("是否正在执行动作")]
    [SerializeField] private bool isPerformingAction = false;
    
    [Tooltip("当前动作执行的倒计时")]
    [SerializeField] private float actionTimer = 0f;

    /// <summary>
    /// 生命周期
    /// </summary>
    void Awake()
    {
        animator = GetComponent<Animator>();
        
        if (animator == null)
        {
            GameLogger.LogError("[Awake] Player Shadow - Animator组件获取失败！", "PlayerShadow");
        }
    }

    void Start()
    {
        // 使用单例引用 PathRecorder（不再需要手动拖拽或查找）
        if (pathRecorder == null)
        {
            GameLogger.Log("PlayerShadow: pathRecorder 未手动赋值，使用单例 Instance", "PlayerShadow");
            pathRecorder = PlayerPathRecorder.Instance;
        }
        
        if (pathRecorder != null)
        {
            GameLogger.Log($"PlayerShadow: ✅ pathRecorder 已设置，实例ID: {pathRecorder.GetInstanceID()}, IsPersistent: {pathRecorder.IsPersistent}", "PlayerShadow");
        }
        else
        {
            GameLogger.LogError("PlayerShadow: ❌ 无法获取 PathRecorder（Instance 为 null）", "PlayerShadow");
        }
        
        // 初始化为Idle状态
        ForcePlayIdle();
        
        // 延迟检查路径恢复（确保PathRecorder的Awake已执行）
        Invoke(nameof(CheckPathRecovery), 0.1f);
    }
    
    /// <summary>
    /// 检查路径恢复情况（调试用）
    /// </summary>
    void CheckPathRecovery()
    {
        GameLogger.Log($"PlayerShadow: CheckPathRecovery 开始", "PlayerShadow");
        GameLogger.Log($"PlayerShadow: pathRecorder == null ? {pathRecorder == null}", "PlayerShadow");
        GameLogger.Log($"PlayerShadow: PlayerPathRecorder.Instance == null ? {PlayerPathRecorder.Instance == null}", "PlayerShadow");
        
        // 如果 pathRecorder 仍然为 null，尝试使用单例
        if (pathRecorder == null && PlayerPathRecorder.Instance != null)
        {
            pathRecorder = PlayerPathRecorder.Instance;
            GameLogger.Log($"PlayerShadow: ✅ 从单例获取 PathRecorder，实例ID: {pathRecorder.GetInstanceID()}", "PlayerShadow");
        }
        
        if (pathRecorder != null)
        {
            int pathLength = pathRecorder.GetMaxPathLength();
            GameLogger.Log($"PlayerShadow: pathRecorder 实例ID: {pathRecorder.GetInstanceID()}, IsPersistent: {pathRecorder.IsPersistent}", "PlayerShadow");
            
            if (pathLength > 0)
            {
                GameLogger.Log($"PlayerShadow: ✅ 检测到路径数据已恢复，长度：{pathLength}", "PlayerShadow");
            }
            else
            {
                GameLogger.Log("PlayerShadow: 路径为空（首次游戏或EndGame后）", "PlayerShadow");
            }
        }
        else
        {
            GameLogger.LogError("PlayerShadow: ❌ 仍然无法找到 PathRecorder！", "PlayerShadow");
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

    /// <summary>
    /// 开始播放影子序列（由GameManager调用）
    /// </summary>
    public void StartShadowSequence()
    {
        GameLogger.Log($"PlayerShadow: ========== StartShadowSequence 被调用 ==========", "PlayerShadow");
        GameLogger.Log($"PlayerShadow: pathRecorder == null ? {pathRecorder == null}", "PlayerShadow");
        
        if (pathRecorder == null)
        {
            // 尝试从单例获取
            pathRecorder = PlayerPathRecorder.Instance;
            GameLogger.Log($"PlayerShadow: 从单例重新获取 PathRecorder，Instance == null ? {PlayerPathRecorder.Instance == null}", "PlayerShadow");
            
            if (pathRecorder == null)
            {
                GameLogger.LogError("PlayerShadowController: PathRecorder未赋值，无法启动影子！", "PlayerShadow");
                return;
            }
        }
        
        GameLogger.Log($"PlayerShadow: 使用 PathRecorder 实例ID: {pathRecorder.GetInstanceID()}, 路径长度: {pathRecorder.GetMaxPathLength()}", "PlayerShadow");

        // 复制路径记录器中的最远路径
        CopyShadowSequenceFromPathRecorder();

        // 如果路径为空，保持Idle状态
        if (shadowSequence.Count == 0)
        {
            GameLogger.Log("PlayerShadow: 路径为空（首次游戏或EndGame后），保持Idle状态", "PlayerShadow");
            ForcePlayIdle();
            return;
        }

        isPlaying = true;
        currentActionIndex = 0;
        ExecuteCurrentAction();
        
        GameLogger.Log($"PlayerShadow: ✅ 开始播放影子序列，共 {shadowSequence.Count} 个动作", "PlayerShadow");
    }

    /// <summary>
    /// 停止播放影子序列
    /// </summary>
    public void StopShadowSequence()
    {
        isPlaying = false;
        isPerformingAction = false;
        CancelInvoke();
        ForcePlayIdle();
        
        GameLogger.Log("PlayerShadow: 停止播放影子序列", "PlayerShadow");
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
    }

    /// <summary>
    /// 从PathRecorder复制影子序列
    /// </summary>
    void CopyShadowSequenceFromPathRecorder()
    {
        shadowSequence.Clear();
        
        if (pathRecorder == null)
        {
            GameLogger.LogError("PlayerShadow: pathRecorder 为 null，无法复制路径！", "PlayerShadow");
            return;
        }

        GameLogger.Log($"PlayerShadow: 开始复制路径，PathRecorder 实例ID: {pathRecorder.GetInstanceID()}, IsPersistent: {pathRecorder.IsPersistent}", "PlayerShadow");

        List<PlayerAction> maxPath = pathRecorder.GetMaxPath();
        
        GameLogger.Log($"PlayerShadow: GetMaxPath() 返回的路径长度: {maxPath.Count}", "PlayerShadow");
        
        // 打印源路径内容（调试用）
        if (maxPath.Count > 0)
        {
            string pathString = "源路径内容：";
            for (int i = 0; i < maxPath.Count; i++)
            {
                pathString += $"[{i}]{maxPath[i].attackType}";
                if (i < maxPath.Count - 1) pathString += " → ";
            }
            GameLogger.Log(pathString, "PlayerShadow");
        }
        
        // 深拷贝路径数据
        foreach (var action in maxPath)
        {
            shadowSequence.Add(new PlayerAction
            {
                attackType = action.attackType,
                timestamp = action.timestamp
            });
        }
        
        // 打印复制后的路径（验证）
        if (shadowSequence.Count > 0)
        {
            string copiedPathString = "复制后的路径：";
            for (int i = 0; i < shadowSequence.Count; i++)
            {
                copiedPathString += $"[{i}]{shadowSequence[i].attackType}";
                if (i < shadowSequence.Count - 1) copiedPathString += " → ";
            }
            GameLogger.Log(copiedPathString, "PlayerShadow");
        }
        
        GameLogger.Log($"PlayerShadow: ✅ 复制路径成功，共 {shadowSequence.Count} 个动作", "PlayerShadow");
    }

    /// <summary>
    /// 执行当前动作
    /// </summary>
    void ExecuteCurrentAction()
    {
        if (!isPlaying || shadowSequence.Count == 0) return;
        if (currentActionIndex >= shadowSequence.Count)
        {
            GameLogger.Log("PlayerShadow: 影子序列播放完成", "PlayerShadow");
            StopShadowSequence();
            return;
        }

        PlayerAction currentAction = shadowSequence[currentActionIndex];
        isPerformingAction = true;
        
        // 获取动画时长
        AnimationClip clip = GetAnimationClip(currentAction.attackType);
        actionTimer = clip != null ? clip.length : actionDuration;

        // 播放对应的动画
        PlayActionAnimation(currentAction.attackType);

        GameLogger.Log($"PlayerShadow: 执行动作 [{currentActionIndex}] {currentAction.attackType}，持续时间 {actionTimer:F2} 秒", "PlayerShadow");
    }

    /// <summary>
    /// 移动到下一个动作
    /// </summary>
    void ExecuteNextAction()
    {
        if (!isPlaying) return;

        currentActionIndex++;

        // 检查是否到达序列末尾
        if (currentActionIndex >= shadowSequence.Count)
        {
            GameLogger.Log("PlayerShadow: 影子序列播放完成", "PlayerShadow");
            StopShadowSequence();
        }
        else
        {
            ExecuteCurrentAction();
        }
    }

    /// <summary>
    /// 根据攻击类型获取对应的动画片段
    /// </summary>
    AnimationClip GetAnimationClip(AttackType attackType)
    {
        switch (attackType)
        {
            case AttackType.AttackX:
                return attackXAnimation;
            case AttackType.AttackY:
                return attackYAnimation;
            case AttackType.AttackB:
                return attackBAnimation;
            default:
                return idleAnimation;
        }
    }

    /// <summary>
    /// 根据攻击类型播放对应的动画
    /// </summary>
    void PlayActionAnimation(AttackType attackType)
    {
        if (animator == null) return;

        AnimationClip clipToPlay = GetAnimationClip(attackType);

        if (ComponentValidator.ValidateAndLogClip(clipToPlay, attackType.ToString(), "PlayerShadow"))
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
    /// 编辑器验证
    /// </summary>
    void OnValidate()
    {
        if (!Application.isPlaying && !enabled)
        {
            enabled = true;
        }
    }
}
