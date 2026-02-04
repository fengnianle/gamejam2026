using UnityEngine;
using System.Collections;

/// <summary>
/// 脚本演出管理器
/// 负责管理游戏中的所有脚本演出（开场、重新开始、胜利、失败等）
/// 使用方法：
/// 1. 在场景中创建Empty GameObject命名为CutsceneManager
/// 2. 挂载此脚本
/// 3. 在Animator中配置各种演出动画
/// 4. 在Animation的关键帧上添加Event调用相应的回调方法
/// </summary>
public class CutsceneManager : MonoBehaviour
{
    /// <summary>
    /// 单例实例（Scene内单例）
    /// </summary>
    public static CutsceneManager Instance { get; private set; }

    /// <summary>
    /// [!] 必须拖拽赋值的场景对象引用
    /// </summary>
    [Space(10)]
    [Header("[!] 场景对象引用 - 必须手动拖拽赋值 [!]")]
    [Space(5)]
    [Tooltip("[!] 必须赋值：演出动画控制器（请在Inspector中拖拽赋值）")]
    public Animator cutsceneAnimator;
    
    [Tooltip("[!] 必须赋值：玩家对象（用于演出中的移动等操作，请在Inspector中拖拽赋值）")]
    public GameObject playerObject;
    
    [Tooltip("[!] 必须赋值：Boss对象（用于演出中的移动等操作，请在Inspector中拖拽赋值）")]
    public GameObject bossObject;

    /// <summary>
    /// 初始位置配置
    /// </summary>
    [Space(10)]
    [Header("初始位置配置")]
    [Tooltip("Player初始X坐标位置")]
    public float playerInitialPositionX = -7f;
    
    [Tooltip("Boss初始X坐标位置")]
    public float bossInitialPositionX = 7f;

    /// <summary>
    /// 动画状态名称配置
    /// </summary>
    [Space(10)]
    [Header("动画状态名称配置")]
    [Tooltip("初始位置动画的状态名称（单帧动画，用于重置Player和Boss到初始位置）")]
    public string initialPositionState = "InitialPosition";
    
    [Tooltip("开场演出动画的状态名称")]
    public string openingCutsceneState = "OpeningCutscene";
    
    [Tooltip("比赛开始演出动画的状态名称")]
    public string gameStartCutsceneState = "GameStart";
    
    [Tooltip("重新开始演出动画的状态名称")]
    public string restartCutsceneState = "Restart";
    
    [Tooltip("胜利演出动画的状态名称")]
    public string victoryCutsceneState = "Victory";
    
    [Tooltip("失败演出动画的状态名称")]
    public string defeatCutsceneState = "Defeat";

    /// <summary>
    /// 状态标记
    /// </summary>
    [Space(10)]
    [Header("运行时状态")]
    [SerializeField]
    [Tooltip("当前是否正在播放演出")]
    private bool isPlayingCutscene = false;

    /// <summary> ----------------------------------------- 生命周期 ----------------------------------------- </summary>
    void Awake()
    {
        // 单例模式
        if (Instance != null && Instance != this)
        {
            Debug.LogError("场景中存在多个CutsceneManager！请确保场景中只有一个CutsceneManager。");
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        // 验证引用
        if (cutsceneAnimator == null)
        {
            GameLogger.LogWarning("CutsceneManager: cutsceneAnimator未赋值！请在Inspector中拖拽赋值", "CutsceneManager");
        }
    }

    /// <summary> ----------------------------------------- 公共方法（供GameManager调用） ----------------------------------------- </summary>
    /// <summary>
    /// 播放开场演出（入场动画完成后调用）
    /// 演出内容：Player和Boss从场景两边移动到中央
    /// </summary>
    public void PlayOpeningCutscene()
    {
        GameLogger.Log("CutsceneManager: 开始播放开场演出", "CutsceneManager");
        
        if (cutsceneAnimator != null)
        {
            isPlayingCutscene = true;
            cutsceneAnimator.Play(openingCutsceneState);
        }
        else
        {
            GameLogger.LogError("CutsceneManager: cutsceneAnimator为null，无法播放演出！", "CutsceneManager");
            // 如果没有动画，直接通知游戏开始
            OnOpeningCutsceneComplete();
        }
    }

    /// <summary>
    /// 播放比赛开始演出
    /// </summary>
    public void PlayGameStartCutscene()
    {
        GameLogger.Log("CutsceneManager: 开始播放比赛开始演出", "CutsceneManager");
        
        if (cutsceneAnimator != null)
        {
            isPlayingCutscene = true;
            cutsceneAnimator.Play(gameStartCutsceneState);
        }
        else
        {
            // 如果没有动画，直接通知游戏开始
            OnGameStartCutsceneComplete();
        }
    }

    /// <summary>
    /// 播放重新开始演出
    /// </summary>
    public void PlayRestartCutscene()
    {
        GameLogger.Log("CutsceneManager: 开始播放重新开始演出", "CutsceneManager");
        
        if (cutsceneAnimator != null)
        {
            isPlayingCutscene = true;
            cutsceneAnimator.Play(restartCutsceneState);
        }
        else
        {
            // 如果没有动画，直接通知重新开始完成
            OnRestartCutsceneComplete();
        }
    }

    /// <summary>
    /// 播放胜利演出
    /// </summary>
    public void PlayVictoryCutscene()
    {
        GameLogger.Log("CutsceneManager: 开始播放胜利演出", "CutsceneManager");
        
        if (cutsceneAnimator != null)
        {
            isPlayingCutscene = true;
            cutsceneAnimator.Play(victoryCutsceneState);
        }
        else
        {
            // 如果没有动画，直接通知胜利演出完成
            OnVictoryCutsceneComplete();
        }
    }

    /// <summary>
    /// 播放失败演出
    /// </summary>
    public void PlayDefeatCutscene()
    {
        GameLogger.Log("CutsceneManager: 开始播放失败演出", "CutsceneManager");
        
        if (cutsceneAnimator != null)
        {
            isPlayingCutscene = true;
            cutsceneAnimator.Play(defeatCutsceneState);
        }
        else
        {
            // 如果没有动画，直接通知失败演出完成
            OnDefeatCutsceneComplete();
        }
    }

    /// <summary> ----------------------------------------- Animation Event回调（由动画事件帧调用） ----------------------------------------- </summary>
    /// <summary>
    /// 开场演出完成回调（由Animation Event调用）
    /// 在开场演出的最后一帧触发
    /// </summary>
    public void OnOpeningCutsceneComplete()
    {
        GameLogger.Log("CutsceneManager: 开场演出完成（通过Animation Event触发）", "CutsceneManager");
        
        isPlayingCutscene = false;
        
        // 通知GameManager开场演出已完成，可以开始比赛
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnOpeningCutsceneComplete();
        }
    }

    /// <summary>
    /// 比赛开始演出完成回调（由Animation Event调用）
    /// </summary>
    public void OnGameStartCutsceneComplete()
    {
        GameLogger.Log("CutsceneManager: 比赛开始演出完成（通过Animation Event触发）", "CutsceneManager");
        
        isPlayingCutscene = false;
        
        // 通知GameManager比赛开始演出已完成
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStartCutsceneComplete();
        }
    }

    /// <summary>
    /// 重新开始演出完成回调（由Animation Event调用）
    /// </summary>
    public void OnRestartCutsceneComplete()
    {
        GameLogger.Log("CutsceneManager: 重新开始演出完成（通过Animation Event触发）", "CutsceneManager");
        
        isPlayingCutscene = false;
        
        // 通知GameManager重新开始演出已完成
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnRestartCutsceneComplete();
        }
    }

    /// <summary>
    /// 胜利演出完成回调（由Animation Event调用）
    /// </summary>
    public void OnVictoryCutsceneComplete()
    {
        GameLogger.Log("CutsceneManager: 胜利演出完成（通过Animation Event触发）", "CutsceneManager");
        
        isPlayingCutscene = false;
        
        // 通知GameManager胜利演出已完成
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnVictoryCutsceneComplete();
        }
    }

    /// <summary>
    /// 失败演出完成回调（由Animation Event调用）
    /// </summary>
    public void OnDefeatCutsceneComplete()
    {
        GameLogger.Log("CutsceneManager: 失败演出完成（通过Animation Event触发）", "CutsceneManager");
        
        isPlayingCutscene = false;
        
        // 通知GameManager失败演出已完成
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDefeatCutsceneComplete();
        }
    }

    /// <summary> ----------------------------------------- 辅助方法 ----------------------------------------- </summary>
    /// <summary>
    /// 重置角色位置到初始位置
    /// 由GameManager在ResetGameState时调用
    /// 播放单帧初始位置动画，将Player和Boss设置到初始位置
    /// </summary>
    public void ResetCharacterPositions()
    {
        GameLogger.Log("CutsceneManager: 播放初始位置动画", "CutsceneManager");
        
        if (cutsceneAnimator != null)
        {
            cutsceneAnimator.Play(initialPositionState);
        }
        else
        {
            GameLogger.LogWarning("CutsceneManager: cutsceneAnimator为null，无法重置位置！", "CutsceneManager");
        }
    }
    
    /// <summary>
    /// 检查当前是否正在播放演出
    /// </summary>
    public bool IsPlayingCutscene()
    {
        return isPlayingCutscene;
    }

    /// <summary>
    /// 停止当前演出（紧急停止用）
    /// </summary>
    public void StopCutscene()
    {
        isPlayingCutscene = false;
        GameLogger.Log("CutsceneManager: 演出已停止", "CutsceneManager");
    }
}
