using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 玩家路径记录器
/// 记录玩家在游戏中走过的"最远进度路径"
/// 用于驱动玩家影子系统，展示玩家历史最佳操作序列
/// </summary>
public class PlayerPathRecorder : MonoBehaviour
{
    [Header("路径数据")]
    [Tooltip("玩家的最远进度路径（持久化数据，Restart时保留）")]
    [SerializeField] private List<PlayerAction> playerMaxPath = new List<PlayerAction>();
    
    [Tooltip("当前小局的输入序列（临时数据，每局重置）")]
    [SerializeField] private List<PlayerAction> currentSessionInputs = new List<PlayerAction>();
    
    /// <summary>
    /// 标记：当前对象是否被 DontDestroyOnLoad 保护
    /// </summary>
    [SerializeField] private bool isPersistent = false;
    
    /// <summary>
    /// 单例实例（静态引用，方便其他组件访问）
    /// </summary>
    public static PlayerPathRecorder Instance { get; private set; }
    
    /// <summary>
    /// 获取是否为持久化实例（公共只读属性）
    /// </summary>
    public bool IsPersistent => isPersistent;
    
    [Header("调试信息")]
    [Tooltip("显示详细的路径记录日志")]
    public bool showDebugLogs = true;
    
    /// <summary>
    /// 生命周期：Awake时处理持久化实例逻辑
    /// </summary>
    void Awake()
    {
        if (showDebugLogs)
        {
            GameLogger.Log($"[路径记录] ========== PathRecorder Awake ==========", "PathRecorder");
            GameLogger.Log($"[路径记录] 当前实例ID: {GetInstanceID()}", "PathRecorder");
            GameLogger.Log($"[路径记录] isPersistent: {isPersistent}", "PathRecorder");
            GameLogger.Log($"[路径记录] Instance 是否为 null: {Instance == null}", "PathRecorder");
        }
        
        // 查找所有 PlayerPathRecorder 实例
        PlayerPathRecorder[] allRecorders = FindObjectsOfType<PlayerPathRecorder>();
        
        if (showDebugLogs)
        {
            GameLogger.Log($"[路径记录] 场景中找到 {allRecorders.Length} 个 PathRecorder 实例", "PathRecorder");
        }
        
        // 如果场景中有多个实例，说明有旧的持久化实例存在
        if (allRecorders.Length > 1)
        {
            // 查找持久化实例（标记为 isPersistent 的）
            PlayerPathRecorder persistentInstance = null;
            foreach (var recorder in allRecorders)
            {
                if (recorder.isPersistent && recorder != this)
                {
                    persistentInstance = recorder;
                    break;
                }
            }
            
            if (persistentInstance != null)
            {
                // 找到了旧的持久化实例，销毁当前新实例
                if (showDebugLogs)
                {
                    GameLogger.Log($"[路径记录] ✅ 找到持久化实例(ID:{persistentInstance.GetInstanceID()})，路径长度：{persistentInstance.playerMaxPath.Count}", "PathRecorder");
                    
                    if (persistentInstance.playerMaxPath.Count > 0)
                    {
                        string pathString = "持久化实例的路径：";
                        for (int i = 0; i < persistentInstance.playerMaxPath.Count; i++)
                        {
                            pathString += $"[{i}]{persistentInstance.playerMaxPath[i].attackType}";
                            if (i < persistentInstance.playerMaxPath.Count - 1) pathString += " → ";
                        }
                        GameLogger.Log(pathString, "PathRecorder");
                    }
                    
                    GameLogger.Log($"[路径记录] ⚠️ 销毁当前新实例(ID:{GetInstanceID()})，保留持久化实例", "PathRecorder");
                }
                
                // 销毁当前实例（Instance 保持指向持久化实例）
                Destroy(gameObject);
                return;
            }
            else if (!isPersistent)
            {
                // 没有找到持久化标记的实例，但有多个实例，销毁非持久化的实例
                if (showDebugLogs)
                {
                    GameLogger.Log($"[路径记录] ⚠️ 发现多个实例但无持久化标记，销毁当前实例(ID:{GetInstanceID()})", "PathRecorder");
                }
                Destroy(gameObject);
                return;
            }
        }
        
        // 设置单例引用（只有保留下来的实例才设置）
        Instance = this;
        
        // 如果是唯一实例，或者是持久化实例，保留
        if (showDebugLogs)
        {
            if (isPersistent)
            {
                GameLogger.Log($"[路径记录] ✅ 当前实例是持久化实例，保留数据，长度：{playerMaxPath.Count}", "PathRecorder");
            }
            else
            {
                GameLogger.Log($"[路径记录] 首次创建，初始化实例", "PathRecorder");
            }
            GameLogger.Log($"[路径记录] ✅ 设置 Instance = this (ID:{GetInstanceID()})", "PathRecorder");
        }
    }

    /// <summary>
    /// 记录玩家的输入动作
    /// 在玩家每次按下攻击键时调用
    /// </summary>
    public void RecordInput(AttackType attackType)
    {
        PlayerAction action = new PlayerAction
        {
            attackType = attackType,
            timestamp = Time.time
        };
        
        currentSessionInputs.Add(action);
        
        if (showDebugLogs)
        {
            GameLogger.Log($"[路径记录] 记录玩家输入：{attackType}，当前小局输入数：{currentSessionInputs.Count}", "PathRecorder");
        }
    }

    /// <summary>
    /// 玩家死亡时调用
    /// 将当前小局的输入合并到最远路径中
    /// 逻辑：前面的动作会被替换，如果走得更远则追加新动作
    /// </summary>
    public void OnPlayerDeath()
    {
        int reachedIndex = currentSessionInputs.Count;
        
        if (showDebugLogs)
        {
            GameLogger.Log($"[路径记录] 玩家死亡，本局到达索引：{reachedIndex}，开始更新最远路径", "PathRecorder");
            GameLogger.Log($"[路径记录] 合并前最远路径长度：{playerMaxPath.Count}", "PathRecorder");
        }
        
        // 合并路径逻辑
        for (int i = 0; i < currentSessionInputs.Count; i++)
        {
            if (i < playerMaxPath.Count)
            {
                // 替换已有路径的对应位置
                var oldAction = playerMaxPath[i].attackType;
                var newAction = currentSessionInputs[i].attackType;
                
                if (oldAction != newAction)
                {
                    playerMaxPath[i] = currentSessionInputs[i];
                    if (showDebugLogs)
                    {
                        GameLogger.Log($"[路径记录] 索引{i}：{oldAction} → {newAction}（替换）", "PathRecorder");
                    }
                }
                else
                {
                    playerMaxPath[i] = currentSessionInputs[i]; // 更新时间戳
                    if (showDebugLogs)
                    {
                        GameLogger.Log($"[路径记录] 索引{i}：{oldAction}（保持）", "PathRecorder");
                    }
                }
            }
            else
            {
                // 追加新进度
                playerMaxPath.Add(currentSessionInputs[i]);
                if (showDebugLogs)
                {
                    GameLogger.Log($"[路径记录] 索引{i}：{currentSessionInputs[i].attackType}（新增）", "PathRecorder");
                }
            }
        }
        
        if (showDebugLogs)
        {
            GameLogger.Log($"[路径记录] 合并后最远路径长度：{playerMaxPath.Count}", "PathRecorder");
            LogCurrentPath();
        }
        
        // 合并完成后清空当前小局输入（防止重复合并）
        currentSessionInputs.Clear();
    }

    /// <summary>
    /// Restart时调用
    /// 清空当前小局的输入，但保留最远路径
    /// </summary>
    public void OnRestart()
    {
        currentSessionInputs.Clear();
        
        if (showDebugLogs)
        {
            GameLogger.Log($"[路径记录] Restart - 清空当前小局输入，保留最远路径（长度：{playerMaxPath.Count}）", "PathRecorder");
        }
    }
    
    /// <summary>
    /// 准备Restart（场景重载前调用）
    /// 使用 DontDestroyOnLoad 保护当前实例，使其在场景重载后仍然存在
    /// </summary>
    public void PrepareForRestart()
    {
        if (showDebugLogs)
        {
            GameLogger.Log($"[路径记录] ========== PrepareForRestart 开始 ==========", "PathRecorder");
            GameLogger.Log($"[路径记录] 实例ID: {GetInstanceID()}", "PathRecorder");
            GameLogger.Log($"[路径记录] GameObject名称: {gameObject.name}", "PathRecorder");
            GameLogger.Log($"[路径记录] 父对象: {(transform.parent == null ? "无(根对象)" : transform.parent.name)}", "PathRecorder");
            GameLogger.Log($"[路径记录] playerMaxPath 长度：{playerMaxPath.Count}", "PathRecorder");
            GameLogger.Log($"[路径记录] currentSessionInputs 长度：{currentSessionInputs.Count}", "PathRecorder");
            
            // 打印路径内容（添加异常处理）
            if (playerMaxPath.Count > 0)
            {
                try
                {
                    string pathString = "当前路径内容：";
                    for (int i = 0; i < playerMaxPath.Count; i++)
                    {
                        if (playerMaxPath[i] == null)
                        {
                            pathString += $"[{i}]null";
                            GameLogger.LogError($"⚠️ playerMaxPath[{i}] 为 null！", "PathRecorder");
                        }
                        else
                        {
                            pathString += $"[{i}]{playerMaxPath[i].attackType}";
                        }
                        
                        if (i < playerMaxPath.Count - 1) pathString += " → ";
                    }
                    GameLogger.Log(pathString, "PathRecorder");
                }
                catch (System.Exception ex)
                {
                    GameLogger.LogError($"打印路径时发生异常：{ex.Message}\n{ex.StackTrace}", "PathRecorder");
                }
            }
            else
            {
                GameLogger.Log("[路径记录] playerMaxPath 为空，无路径内容", "PathRecorder");
            }
        }
        
        // 注意：OnPlayerDeath() 已经在 PlayerController.Die() 中被调用过了
        // currentSessionInputs 应该已经被合并并清空
        
        // 标记为持久化实例
        isPersistent = true;
        
        // ⚠️ 重要：如果有父对象，必须先解除父子关系，否则 DontDestroyOnLoad 会失效！
        if (transform.parent != null)
        {
            if (showDebugLogs)
            {
                GameLogger.Log($"[路径记录] ⚠️ 检测到父对象 '{transform.parent.name}'，解除父子关系", "PathRecorder");
            }
            transform.SetParent(null);
        }
        
        // 使用 DontDestroyOnLoad 保护当前对象，使其在场景重载后仍然存在
        DontDestroyOnLoad(gameObject);
        
        if (showDebugLogs)
        {
            GameLogger.Log($"[路径记录] ✅ PrepareForRestart 完成 - 对象已标记为 DontDestroyOnLoad + isPersistent", "PathRecorder");
            GameLogger.Log($"[路径记录] 当前场景: {gameObject.scene.name}", "PathRecorder");
        }
    }

    /// <summary>
    /// EndGame时调用
    /// 完全重置所有路径数据
    /// </summary>
    public void OnEndGame()
    {
        currentSessionInputs.Clear();
        playerMaxPath.Clear();
        
        if (showDebugLogs)
        {
            GameLogger.Log("[路径记录] EndGame - 完全重置所有路径数据", "PathRecorder");
        }
    }
    
    /// <summary>
    /// 清空所有路径数据（包括持久化标记）
    /// </summary>
    public void ClearAllPathData()
    {
        currentSessionInputs.Clear();
        playerMaxPath.Clear();
        
        // 清理持久化标记
        isPersistent = false;
        
        if (showDebugLogs)
        {
            GameLogger.Log("[路径记录] 清空所有路径数据（包括持久化标记）", "PathRecorder");
        }
    }

    /// <summary>
    /// 获取最远路径（只读）
    /// 供PlayerShadowController使用
    /// </summary>
    public List<PlayerAction> GetMaxPath()
    {
        return playerMaxPath;
    }

    /// <summary>
    /// 获取当前小局的输入数量
    /// </summary>
    public int GetCurrentSessionInputCount()
    {
        return currentSessionInputs.Count;
    }

    /// <summary>
    /// 获取最远路径的长度
    /// </summary>
    public int GetMaxPathLength()
    {
        return playerMaxPath.Count;
    }

    /// <summary>
    /// 调试：打印当前路径
    /// </summary>
    void LogCurrentPath()
    {
        if (playerMaxPath.Count == 0)
        {
            GameLogger.Log("[路径记录] 当前最远路径：空", "PathRecorder");
            return;
        }
        
        string pathString = "[路径记录] 当前最远路径：";
        for (int i = 0; i < playerMaxPath.Count; i++)
        {
            pathString += $"[{i}]{playerMaxPath[i].attackType}";
            if (i < playerMaxPath.Count - 1) pathString += " → ";
        }
        
        GameLogger.Log(pathString, "PathRecorder");
    }

    /// <summary>
    /// 对象销毁时调用（用于调试）
    /// </summary>
    void OnDestroy()
    {
        if (showDebugLogs)
        {
            GameLogger.Log($"[路径记录] ========== OnDestroy 被调用 ==========", "PathRecorder");
            GameLogger.Log($"[路径记录] 实例ID: {GetInstanceID()}", "PathRecorder");
            GameLogger.Log($"[路径记录] GameObject名称: {gameObject.name}", "PathRecorder");
            GameLogger.Log($"[路径记录] isPersistent: {isPersistent}", "PathRecorder");
            GameLogger.Log($"[路径记录] playerMaxPath 长度: {playerMaxPath.Count}", "PathRecorder");
        }
        
        // 如果销毁的是当前单例实例，清空引用
        if (Instance == this)
        {
            if (showDebugLogs)
            {
                GameLogger.Log($"[路径记录] ⚠️ 当前实例是 Instance，清空静态引用", "PathRecorder");
            }
            Instance = null;
        }
    }
    
    /// <summary>
    /// 编辑器验证
    /// </summary>
    void OnValidate()
    {
        // 确保在编辑器模式下脚本是启用的
        if (!Application.isPlaying && !enabled)
        {
            enabled = true;
        }
    }
}

/// <summary>
/// 玩家动作数据结构
/// 记录玩家的攻击类型和时间戳
/// </summary>
[System.Serializable]
public class PlayerAction
{
    [Tooltip("攻击类型")]
    public AttackType attackType;
    
    [Tooltip("动作发生的时间戳")]
    public float timestamp;
}
