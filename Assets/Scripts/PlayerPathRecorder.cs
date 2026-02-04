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
    /// 单例实例（Scene内单例，不跨Scene持久化）
    /// </summary>
    public static PlayerPathRecorder Instance { get; private set; }
    
    [Header("调试信息")]
    [Tooltip("显示详细的路径记录日志")]
    public bool showDebugLogs = false;
    
    /// <summary>
    /// 生命周期：Awake时初始化单例
    /// </summary>
    void Awake()
    {
        // 简单单例模式（Scene内唯一）
        if (Instance != null && Instance != this)
        {
            Debug.LogError("场景中存在多个PlayerPathRecorder！请确保场景中只有一个PlayerPathRecorder。");
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        if (showDebugLogs)
        {
            GameLogger.Log($"[路径记录] 初始化成功 (ID:{GetInstanceID()})", "PathRecorder");
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
        
        if (showDebugLogs)
        {
            GameLogger.Log("[路径记录] 清空所有路径数据", "PathRecorder");
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
    /// 对象销毁时调用
    /// </summary>
    void OnDestroy()
    {
        // 如果销毁的是当前单例实例，清空引用
        if (Instance == this)
        {
            if (showDebugLogs)
            {
                GameLogger.Log("[路径记录] 单例被销毁", "PathRecorder");
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
