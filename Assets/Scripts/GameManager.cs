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
    
    // 状态标记 (使用静态变量确保跨场景/实例的绝对持久性)
    private static bool isRestarting = false;
    private static bool isEndGame = false;

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
    
    [Tooltip("玩家路径记录器（用于影子系统，请在Inspector中拖拽赋值）")]
    public PlayerPathRecorder pathRecorder;
    
    [Tooltip("玩家影子控制器（可选，用于影子系统）")]
    public PlayerShadowController playerShadowController;

    /// <summary>
    /// UI引用
    /// </summary>
    [Header("UI引用")]
    [Tooltip("开始游戏按钮（请在Inspector中拖拽赋值）")]
    public GameObject startButton;
    
    [Tooltip("重新开始按钮（玩家失败时显示，请在Inspector中拖拽赋值）")]
    public GameObject restartButton;
    
    [Tooltip("结束游戏按钮（游戏结束时显示，请在Inspector中拖拽赋值）")]
    public GameObject endButton;

    /// <summary> ----------------------------------------- 生命周期 ----------------------------------------- </summary>
    void Awake()
    {
        // 单例模式实现
        if (Instance != null && Instance != this)
        {
            // 如果已经存在持久化的Instance，则将当前场景中的引用更新到Instance中
            // 因为当场景重新加载时，新场景中的GameManager会携带该场景特有的对象引用（如UI、Player等）
            // 我们需要把这些新引用赋给那个"活下来"的老GameManager
            Instance.playerController = playerController;
            Instance.bossController = bossController;
            // PathRecorder 也是持久化的单例，无需重新赋值，或者保持指向 Exiting Instance
            Instance.pathRecorder = PlayerPathRecorder.Instance; 
            Instance.playerShadowController = playerShadowController;
            Instance.startButton = startButton;
            Instance.restartButton = restartButton;
            Instance.endButton = endButton;
            
            // 重新绑定UI事件到持久化实例
            Instance.RegisterButtonEvents();
            
            // 如果不是重启状态，才初始化UI（重启时由OnSceneLoaded处理进入游戏状态）
            // 注意：isRestarting 现在是静态变量
            if (!isRestarting)
            {
                Instance.InitializeUIState();
            }
            
            // 销毁当前这个新生成的"工具人"GameManager
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        // 立即初始化UI状态，确保只显示开始面板
        InitializeUIState();
        // 首次绑定事件
        RegisterButtonEvents();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            
            // 取消所有延迟调用，避免在场景关闭时产生"未清理对象"的警告
            CancelInvoke();
        }
    }

    void Start()
    {
        // 只有持久化的 Instance 才执行初始化逻辑
        // 避免新场景中的"工具人" GameManager 在销毁前干扰 UI 状态
        if (Instance != this) return;
        
        // 首次启动：初始化为主菜单
        ChangeState(GameState.MainMenu);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 延迟处理，确保所有新场景对象的 Awake 都执行完毕
        // 避免在某些"工具人"实例的 Awake 执行前就重置标记
        Invoke(nameof(ProcessSceneLoadedFlags), 0.01f);
    }
    
    /// <summary>
    /// 处理场景加载后的标记（延迟调用）
    /// </summary>
    void ProcessSceneLoadedFlags()
    {
        // 检查是否是Restart或EndGame
        if (isRestarting)
        {
            // Restart：直接进入游戏，保留路径数据
            isRestarting = false;
            ChangeState(GameState.Playing);
        }
        else if (isEndGame)
        {
            // EndGame：完全重置，清空路径数据
            isEndGame = false;
            if (pathRecorder != null)
            {
                pathRecorder.ClearAllPathData();
            }
            ChangeState(GameState.MainMenu);
        }
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
    /// Restart保留玩家的最远路径，影子会播放历史路径
    /// </summary>
    public void RestartGame()
    {
        // 设置Restart标记（场景重载后直接进入Playing状态）
        isRestarting = true;
        
        // 重新加载当前场景
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// 结束游戏，返回主菜单（由UI按钮调用）
    /// EndGame完全重置游戏，清空玩家的最远路径
    /// </summary>
    public void EndGame()
    {
        // 设置EndGame标记（场景重载后完全重置）
        isEndGame = true;
        
        // 重新加载当前场景
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    /// <summary>
    /// 返回主菜单（向后兼容方法，调用EndGame）
    /// </summary>
    public void ReturnToMainMenu()
    {
        EndGame();
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
    /// 注册按钮事件（解决场景重载后按钮引用失效的问题）
    /// </summary>
    public void RegisterButtonEvents()
    {
        // 绑定 Restart 按钮
        BindButton(restartButton, RestartGame);

        // 绑定 End 按钮
        BindButton(endButton, EndGame);
        
        // 绑定 Start 按钮
        BindButton(startButton, StartGame);
    }

    /// <summary>
    /// 辅助方法：绑定按钮事件
    /// </summary>
    void BindButton(GameObject go, UnityEngine.Events.UnityAction action)
    {
        if (go != null)
        {
            Button btn = go.GetComponent<Button>();
            if (btn == null) btn = go.GetComponentInChildren<Button>(true); // 宽容查找
            
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(action);
            }
        }
    }

    /// <summary>
    /// 初始化UI状态（在Awake中调用，确保启动时UI正确）
    /// </summary>
    void InitializeUIState()
    {
        // 只显示开始游戏按钮，隐藏其他按钮
        SetUIActive(startButton, true);
        SetUIActive(restartButton, false);
        SetUIActive(endButton, false);
        
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
        
        // 显示开始游戏按钮，隐藏其他按钮
        SetUIActive(startButton, true);
        SetUIActive(restartButton, false);
        SetUIActive(endButton, false);
        
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
        
        // 隐藏所有UI
        SetUIActive(startButton, false);
        SetUIActive(restartButton, false);
        SetUIActive(endButton, false);
        
        // 启用玩家输入
        if (playerController != null)
        {
            playerController.SetInputEnabled(true);
        }
        
        // 延迟启动Boss和Player影子，确保所有组件已初始化
        Invoke(nameof(StartGameSequences), 0.2f);
    }
    
    /// <summary>
    /// 启动游戏序列（Boss和Player影子）
    /// </summary>
    void StartGameSequences()
    {
        // 启动Boss攻击序列（包含Boss影子）
        if (bossController != null)
        {
            bossController.StartSequence();
            GameLogger.Log("GameManager: 已启动Boss序列", "GameManager");
        }
        
        // 启动Player影子
        if (playerShadowController != null)
        {
            // Player影子与Boss影子一样，提前leadTime秒开始
            Invoke(nameof(StartPlayerShadow), playerShadowController.leadTime);
            GameLogger.Log($"GameManager: 将在{playerShadowController.leadTime}秒后启动Player影子", "GameManager");
        }
    }
    
    /// <summary>
    /// 启动Player影子（延迟调用）
    /// </summary>
    void StartPlayerShadow()
    {
        if (playerShadowController != null)
        {
            playerShadowController.StartShadowSequence();
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
        
        // Boss死亡，只显示End按钮
        SetUIActive(startButton, false);
        SetUIActive(restartButton, false);
        SetUIActive(endButton, true);
        
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
        
        // 玩家死亡，显示Restart和End按钮
        SetUIActive(startButton, false);
        SetUIActive(restartButton, true);
        SetUIActive(endButton, true);
        
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
