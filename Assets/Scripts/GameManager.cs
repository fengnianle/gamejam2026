using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏管理器（单例模式）
/// 负责管理整个游戏的生命周期、状态转换和游戏流程
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>
    /// 单例实例
    /// </summary>
    public static GameManager Instance { get; private set; }

    /// <summary>
    /// 游戏状态
    /// </summary>
    [Header("游戏状态")]
    [Tooltip("当前游戏状态（仅供调试查看）")]
    [SerializeField]
    private GameState currentState = GameState.MainMenu;
    
    /// <summary>
    /// 场景对象引用
    /// </summary>
    [Header("场景对象引用")]
    [Tooltip("玩家控制器（请在Inspector中拖拽赋值）")]
    public PlayerController playerController;
    
    [Tooltip("Boss控制器（请在Inspector中拖拽赋值）")]
    public BossController bossController;

    /// <summary>
    /// UI引用
    /// </summary>
    [Header("UI引用")]
    [Tooltip("开始游戏UI面板（请在Inspector中拖拽赋值）")]
    public GameObject startGamePanel;
    
    [Tooltip("游戏进行中UI面板（请在Inspector中拖拽赋值）")]
    public GameObject gameplayPanel;
    
    [Tooltip("胜利UI面板（请在Inspector中拖拽赋值）")]
    public GameObject victoryPanel;
    
    [Tooltip("失败UI面板（请在Inspector中拖拽赋值）")]
    public GameObject defeatPanel;

    /// <summary> ----------------------------------------- 生命周期 ----------------------------------------- </summary>
    void Awake()
    {
        // 单例模式实现
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            GameLogger.LogWarning("GameManager实例已存在，销毁重复实例。", "GameManager");
            return;
        }
        
        Instance = this;
        // DontDestroyOnLoad(gameObject); // 如果需要跨场景保持GameManager，取消注释此行
        
        // 立即初始化UI状态，确保只显示开始面板
        InitializeUIState();
        
        GameLogger.Log("GameManager初始化成功", "GameManager");
    }

    void Start()
    {
        // 初始化游戏状态
        ChangeState(GameState.MainMenu);
    }

    /// <summary> ----------------------------------------- 公共方法 ----------------------------------------- </summary>
    /// <summary>
    /// 开始游戏（由UI按钮调用）
    /// </summary>
    public void StartGame()
    {
        GameLogger.Log("开始游戏", "GameManager");
        ChangeState(GameState.Playing);
    }

    /// <summary>
    /// 玩家死亡回调（由PlayerController调用）
    /// </summary>
    public void OnPlayerDeath()
    {
        GameLogger.Log("玩家死亡，游戏失败", "GameManager");
        ChangeState(GameState.Defeat);
    }

    /// <summary>
    /// Boss死亡回调（由BossController调用）
    /// </summary>
    public void OnBossDeath()
    {
        GameLogger.Log("Boss死亡，游戏胜利", "GameManager");
        ChangeState(GameState.Victory);
    }

    /// <summary>
    /// 重新开始游戏（由UI按钮调用）
    /// </summary>
    public void RestartGame()
    {
        GameLogger.Log("重新开始游戏", "GameManager");
        // 重新加载当前场景
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// 返回主菜单（由UI按钮调用）
    /// </summary>
    public void ReturnToMainMenu()
    {
        GameLogger.Log("返回主菜单", "GameManager");
        // 如果有主菜单场景，加载主菜单场景
        // SceneManager.LoadScene("MainMenu");
        
        // 如果只是重置当前场景
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// 退出游戏（由UI按钮调用）
    /// </summary>
    public void QuitGame()
    {
        GameLogger.Log("退出游戏", "GameManager");
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    /// <summary> ----------------------------------------- 私有方法 ----------------------------------------- </summary>
    /// <summary>
    /// 初始化UI状态（在Awake中调用，确保启动时UI正确）
    /// </summary>
    void InitializeUIState()
    {
        // 只显示开始游戏面板
        SetUIActive(startGamePanel, true);
        SetUIActive(gameplayPanel, false);
        SetUIActive(victoryPanel, false);
        SetUIActive(defeatPanel, false);
        
        GameLogger.Log("UI初始状态设置完成", "GameManager");
    }
    
    /// <summary>
    /// 改变游戏状态
    /// </summary>
    void ChangeState(GameState newState)
    {
        if (currentState == newState) return;
        
        // 退出当前状态
        ExitState(currentState);
        
        // 更新状态
        GameState oldState = currentState;
        currentState = newState;
        GameLogger.Log($"游戏状态改变: {oldState} -> {newState}", "GameManager");
        
        // 进入新状态
        EnterState(newState);
    }

    /// <summary>
    /// 进入新状态
    /// </summary>
    void EnterState(GameState state)
    {
        switch (state)
        {
            case GameState.MainMenu:
                OnEnterMainMenu();
                break;
            case GameState.Playing:
                OnEnterPlaying();
                break;
            case GameState.Victory:
                OnEnterVictory();
                break;
            case GameState.Defeat:
                OnEnterDefeat();
                break;
        }
    }

    /// <summary>
    /// 退出当前状态
    /// </summary>
    void ExitState(GameState state)
    {
        switch (state)
        {
            case GameState.MainMenu:
                OnExitMainMenu();
                break;
            case GameState.Playing:
                OnExitPlaying();
                break;
            case GameState.Victory:
                OnExitVictory();
                break;
            case GameState.Defeat:
                OnExitDefeat();
                break;
        }
    }

    /// <summary> ----------------------------------------- 状态处理方法 ----------------------------------------- </summary>
    /// <summary>
    /// 进入主菜单状态
    /// </summary>
    void OnEnterMainMenu()
    {
        GameLogger.Log("进入主菜单状态", "GameManager");
        
        // 显示开始游戏UI
        SetUIActive(startGamePanel, true);
        SetUIActive(gameplayPanel, false);
        SetUIActive(victoryPanel, false);
        SetUIActive(defeatPanel, false);
        
        // 禁用玩家和Boss的行为
        if (playerController != null)
        {
            playerController.SetInputEnabled(false);
            // 确保播放Idle动画
            playerController.ForcePlayIdle();
        }
        
        if (bossController != null)
        {
            bossController.StopSequence();
            // 确保Boss播放Idle动画
            bossController.ForcePlayIdle();
        }
    }

    /// <summary>
    /// 退出主菜单状态
    /// </summary>
    void OnExitMainMenu()
    {
        GameLogger.Log("退出主菜单状态", "GameManager");
    }

    /// <summary>
    /// 进入游戏中状态
    /// </summary>
    void OnEnterPlaying()
    {
        GameLogger.Log("进入游戏中状态", "GameManager");
        
        // 显示游戏进行中UI
        SetUIActive(startGamePanel, false);
        SetUIActive(gameplayPanel, true);
        SetUIActive(victoryPanel, false);
        SetUIActive(defeatPanel, false);
        
        // 启用玩家输入
        if (playerController != null)
        {
            playerController.SetInputEnabled(true);
        }
        
        // 启动Boss攻击序列
        if (bossController != null)
        {
            bossController.StartSequence();
        }
    }

    /// <summary>
    /// 退出游戏中状态
    /// </summary>
    void OnExitPlaying()
    {
        GameLogger.Log("退出游戏中状态", "GameManager");
    }

    /// <summary>
    /// 进入胜利状态
    /// </summary>
    void OnEnterVictory()
    {
        GameLogger.Log("进入胜利状态", "GameManager");
        
        // 显示胜利UI
        SetUIActive(startGamePanel, false);
        SetUIActive(gameplayPanel, false);
        SetUIActive(victoryPanel, true);
        SetUIActive(defeatPanel, false);
        
        // 禁用玩家输入
        if (playerController != null)
        {
            playerController.SetInputEnabled(false);
        }
        
        // 停止Boss序列（Boss已经死亡，这里确保清理）
        if (bossController != null)
        {
            bossController.StopSequence();
        }
        
        // 可以在这里添加：
        // - 播放胜利音效
        // - 显示奖励
        // - 记录游戏数据
    }

    /// <summary>
    /// 退出胜利状态
    /// </summary>
    void OnExitVictory()
    {
        GameLogger.Log("退出胜利状态", "GameManager");
    }

    /// <summary>
    /// 进入失败状态
    /// </summary>
    void OnEnterDefeat()
    {
        GameLogger.Log("进入失败状态", "GameManager");
        
        // 显示失败UI
        SetUIActive(startGamePanel, false);
        SetUIActive(gameplayPanel, false);
        SetUIActive(victoryPanel, false);
        SetUIActive(defeatPanel, true);
        
        // 禁用玩家输入（玩家已经死亡，这里确保清理）
        if (playerController != null)
        {
            playerController.SetInputEnabled(false);
        }
        
        // 停止Boss攻击序列
        if (bossController != null)
        {
            bossController.StopSequence();
        }
        
        // 可以在这里添加：
        // - 播放失败音效
        // - 显示重试提示
        // - 记录游戏数据
    }

    /// <summary>
    /// 退出失败状态
    /// </summary>
    void OnExitDefeat()
    {
        GameLogger.Log("退出失败状态", "GameManager");
    }

    /// <summary>
    /// 安全地设置UI激活状态
    /// </summary>
    void SetUIActive(GameObject uiObject, bool active)
    {
        if (uiObject != null)
        {
            uiObject.SetActive(active);
        }
        else if (active)
        {
            // 只在尝试激活时警告（如果UI为null且需要激活，则发出警告）
            GameLogger.LogWarning($"尝试激活UI对象，但对象为null", "GameManager");
        }
    }

    /// <summary>
    /// 获取当前游戏状态
    /// </summary>
    public GameState GetCurrentState()
    {
        return currentState;
    }

    /// <summary>
    /// 检查游戏是否正在进行中
    /// </summary>
    public bool IsPlaying()
    {
        return currentState == GameState.Playing;
    }
}

/// <summary> ----------------------------------------- 数据类型 ----------------------------------------- </summary>
/// <summary>
/// 游戏状态枚举
/// </summary>
public enum GameState
{
    MainMenu,   // 主菜单（游戏开始前）
    Playing,    // 游戏进行中
    Victory,    // 胜利
    Defeat      // 失败
}
