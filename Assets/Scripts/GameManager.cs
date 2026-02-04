using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏管理器（单例模式）
/// 负责管理整个游戏的生命周期、状态转换和游戏流程
/// 架构说明：
/// - 整个游戏在单个Scene中运行，不进行Scene切换
/// - Restart/EndGame通过重置各组件状态实现，而非销毁重建
/// - 所有引用保持稳定，避免引用丢失问题
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>
    /// 单例实例（Scene内单例，不跨Scene持久化）
    /// </summary>
    public static GameManager Instance { get; private set; }

    /// <summary>
    /// ⚠️ 必须拖拽赋值的场景对象引用
    /// </summary>
    [Space(10)]
    [Header("⚠️ 场景对象引用 - 必须手动拖拽赋值 ⚠️")]
    [Space(5)]
    [Tooltip("⚠️ 必须赋值：UI管理器（请在Inspector中拖拽赋值）")]
    public UIManager uiManager;
    
    [Tooltip("⚠️ 必须赋值：玩家控制器（请在Inspector中拖拽赋值）")]
    public PlayerController playerController;
    
    [Tooltip("⚠️ 必须赋值：Boss控制器（请在Inspector中拖拽赋值）")]
    public BossController bossController;
    
    [Tooltip("⚠️ 必须赋值：玩家路径记录器（用于影子系统，请在Inspector中拖拽赋值）")]
    public PlayerPathRecorder pathRecorder;
    
    [Tooltip("可选：玩家影子控制器（用于影子系统）")]
    public PlayerShadowController playerShadowController;

    /// <summary>
    /// UI元素引用
    /// </summary>
    [Space(10)]
    [Header("⚠️ UI元素引用 - 必须手动拖拽赋值 ⚠️")]
    [Space(5)]
    [Tooltip("⚠️ 必须赋值：开始游戏按钮（请在Inspector中拖拽赋值）")]
    public GameObject startButton;
    
    [Tooltip("⚠️ 必须赋值：重新开始按钮（玩家失败时显示，请在Inspector中拖拽赋值）")]
    public GameObject restartButton;
    
    [Tooltip("⚠️ 必须赋值：结束游戏按钮（游戏结束时显示，请在Inspector中拖拽赋值）")]
    public GameObject endButton;
    
    [Tooltip("⚠️ 必须赋值：玩家血条UI（请在Inspector中拖拽赋值）")]
    public GameObject playerHPBar;
    
    [Tooltip("⚠️ 必须赋值：Boss血条UI（请在Inspector中拖拽赋值）")]
    public GameObject bossHPBar;

    /// <summary>
    /// 游戏状态
    /// </summary>
    [Space(10)]
    [Header("游戏状态")]
    [Tooltip("当前游戏状态（仅供调试查看）")]
    [SerializeField]
    private GameState currentState = GameState.MainMenu;

    /// <summary> ----------------------------------------- 生命周期 ----------------------------------------- </summary>
    void Awake()
    {
        // 简单单例模式（Scene内唯一）
        if (Instance != null && Instance != this)
        {
            Debug.LogError("场景中存在多个GameManager！请确保场景中只有一个GameManager。");
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        // 初始化UI状态（隐藏所有UI，等待动画完成）
        InitializeUIState();
        // 绑定按钮事件
        RegisterButtonEvents();
        // 注册UIManager的动画完成回调
        RegisterUIManagerCallback();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            // 取消所有延迟调用
            CancelInvoke();
        }
    }

    void Start()
    {
        // 游戏启动时不立即进入主菜单，等待UIManager的入场动画播放完成
        // 动画完成后会通过OnAnimationComplete回调显示UI
        // 此时保持currentState为MainMenu（Awake中已初始化）
        GameLogger.Log("GameManager启动，等待入场动画播放完成", "GameManager");
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
    /// 实现方式：重置各组件状态，而非销毁重建
    /// </summary>
    public void RestartGame()
    {
        GameLogger.Log("重新开始游戏（保留最远路径）", "GameManager");
        
        // 记录玩家本局的路径到最远路径（如果走得更远）
        if (pathRecorder != null)
        {
            pathRecorder.OnPlayerDeath(); // 更新最远路径
            pathRecorder.OnRestart();     // 清空当前小局数据
        }
        
        // 重置玩家状态
        if (playerController != null)
        {
            playerController.ResetState();
        }
        
        // 重置Boss状态
        if (bossController != null)
        {
            bossController.ResetState();
        }
        
        // 重置Player影子
        if (playerShadowController != null)
        {
            playerShadowController.ResetState();
        }
        
        // 进入Playing状态
        ChangeState(GameState.Playing);
    }

    /// <summary>
    /// 结束游戏，返回主菜单（由UI按钮调用）
    /// EndGame完全重置游戏，清空玩家的最远路径
    /// 实现方式：重置各组件状态到初始状态
    /// </summary>
    public void EndGame()
    {
        GameLogger.Log("结束游戏，返回主菜单（完全重置）", "GameManager");
        
        // 清空所有路径数据
        if (pathRecorder != null)
        {
            pathRecorder.ClearAllPathData();
        }
        
        // 重置玩家状态
        if (playerController != null)
        {
            playerController.ResetState();
        }
        
        // 重置Boss状态
        if (bossController != null)
        {
            bossController.ResetState();
        }
        
        // 重置Player影子
        if (playerShadowController != null)
        {
            playerShadowController.ResetState();
        }
        
        // 返回主菜单状态
        ChangeState(GameState.MainMenu);
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
    /// 注册UIManager的动画完成回调
    /// </summary>
    void RegisterUIManagerCallback()
    {
        if (uiManager != null)
        {
            // 注册动画完成回调
            uiManager.onAnimationComplete.AddListener(OnAnimationComplete);
            GameLogger.Log("已注册UIManager的动画完成回调", "GameManager");
        }
        else
        {
            GameLogger.LogWarning("UIManager未赋值，将跳过入场动画，直接显示UI", "GameManager");
            // 如果没有UIManager，直接显示UI
            OnAnimationComplete();
        }
    }
    
    /// <summary>
    /// 入场动画播放完成的回调
    /// 显示主菜单UI（Start按钮和血条）
    /// </summary>
    void OnAnimationComplete()
    {
        GameLogger.Log("入场动画播放完成，显示主菜单UI", "GameManager");
        
        // 显示Start按钮
        SetUIActive(startButton, true);
        
        // 显示血条
        SetUIActive(playerHPBar, true);
        SetUIActive(bossHPBar, true);
        
        // 确保Restart和End按钮隐藏
        SetUIActive(restartButton, false);
        SetUIActive(endButton, false);
    }
    
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
    /// 初始时隐藏所有UI，等待入场动画播放完成后再显示
    /// </summary>
    void InitializeUIState()
    {
        // 隐藏所有按钮（等待入场动画完成）
        SetUIActive(startButton, false);
        SetUIActive(restartButton, false);
        SetUIActive(endButton, false);
        
        // 隐藏血条（等待入场动画完成）
        SetUIActive(playerHPBar, false);
        SetUIActive(bossHPBar, false);
        
        GameLogger.Log("UI初始状态设置完成（所有UI已隐藏，等待入场动画）", "GameManager");
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
