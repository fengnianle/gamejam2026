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
    /// [!] 必须拖拽赋值的场景对象引用
    /// </summary>
    [Space(10)]
    [Header("[!] 场景对象引用 - 必须手动拖拽赋值 [!]")]
    [Space(5)]
    [Tooltip("[!] 必须赋值：UI管理器（请在Inspector中拖拽赋值）")]
    public UIManager uiManager;
    
    [Tooltip("[!] 必须赋值：脚本演出管理器（请在Inspector中拖拽赋值）")]
    public CutsceneManager cutsceneManager;
    
    [Tooltip("[!] 必须赋值：玩家控制器（请在Inspector中拖拽赋值）")]
    public PlayerController playerController;
    
    [Tooltip("[!] 必须赋值：Boss控制器（请在Inspector中拖拽赋值）")]
    public BossController bossController;
    
    [Tooltip("[!] 必须赋值：玩家路径记录器（用于影子系统，请在Inspector中拖拽赋值）")]
    public PlayerPathRecorder pathRecorder;
    
    [Tooltip("可选：玩家影子控制器（用于影子系统）")]
    public PlayerShadowController playerShadowController;

    /// <summary>
    /// UI元素引用
    /// </summary>
    [Space(10)]
    [Header("[!] UI元素引用 - 必须手动拖拽赋值 [!]")]
    [Space(5)]
    [Tooltip("[!] 必须赋值：玩家血条UI（请在Inspector中拖拽赋值）")]
    public GameObject playerHPBar;
    
    [Tooltip("[!] 必须赋值：Boss血条UI（请在Inspector中拖拽赋值）")]
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
        
        // 订阅UIManager的Beginning动画完成事件
        SubscribeUIEvents();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            // 取消订阅UI事件
            UnsubscribeUIEvents();
            // 取消所有延迟调用
            CancelInvoke();
        }
    }

    void Start()
    {
        // 游戏启动时不立即进入主菜单，等待入场动画播放完成
        // 入场动画完成后会通过Animation Event调用OnOpeningAnimationComplete()
        // 然后播放开场脚本演出，演出完成后开始比赛
        GameLogger.Log("GameManager启动，等待入场动画和脚本演出", "GameManager");
    }

    /// <summary> ----------------------------------------- 公共方法 ----------------------------------------- </summary>

    /// <summary>
    /// 玩家死亡回调（由PlayerController调用）
    /// </summary>
    public void OnPlayerDeath()
    {
        GameLogger.Log("玩家死亡，游戏失败", "GameManager");
        ChangeState(GameState.Defeat);
        
        // 通知BossController记录玩家看到的最远进度
        if (bossController != null)
        {
            bossController.OnPlayerDeath();
        }
        
        // 播放dieAndRestart动画
        if (uiManager != null)
        {
            uiManager.PlayDieAndRestartAnimation();
        }
        else
        {
            GameLogger.LogWarning("UIManager未赋值，跳过DieAndRestart动画，直接播放开场演出", "GameManager");
            // 如果没有UIManager，直接播放开场演出
            PlayOpeningCutsceneSequence();
        }
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
    /// 重置游戏状态（由dieAndRestart动画中间帧调用）
    /// 重置玩家、Boss的生命值和状态，但不启动游戏序列
    /// </summary>
    public void ResetGameState()
    {
        GameLogger.Log("重置游戏状态（重置生命值和位置）", "GameManager");
        
        // 记录玩家本局的路径到最远路径（如果走得更远）
        if (pathRecorder != null)
        {
            pathRecorder.OnPlayerDeath(); // 更新最远路径
            pathRecorder.OnRestart();     // 清空当前小局数据
        }
        
        // 重置玩家状态（恢复生命值等）
        if (playerController != null)
        {
            playerController.ResetState();
        }
        
        // 重置Boss状态（恢复生命值等）
        if (bossController != null)
        {
            bossController.ResetState();
        }
        
        // 重置Player和Boss的位置（通过CutsceneManager）
        if (cutsceneManager != null)
        {
            cutsceneManager.ResetCharacterPositions();
        }
        
        // 重置Player影子
        if (playerShadowController != null)
        {
            playerShadowController.ResetState();
        }
        
        GameLogger.Log("游戏状态重置完成，等待开场演出后启动游戏", "GameManager");
    }
    
    /// <summary>
    /// 启动游戏序列（由开场演出完成后调用）
    /// 启动Boss攻击序列、玩家影子、启用玩家输入
    /// </summary>
    void StartGameplay()
    {
        GameLogger.Log("启动游戏序列（Boss攻击、玩家输入、影子系统）", "GameManager");
        
        // 确保玩家输入已启用
        if (playerController != null)
        {
            playerController.SetInputEnabled(true);
            GameLogger.Log("玩家输入已启用", "GameManager");
        }
        
        // 进入Playing状态（这会启用玩家输入和启动Boss序列）
        ChangeState(GameState.Playing);
        
        // 确保在状态改变后再次检查并启动序列
        // 延迟启动Boss和Player影子，确保所有组件已初始化
        Invoke(nameof(StartGameSequences), 0.2f);
    }

    /// <summary>
    /// 重新开始游戏（向后兼容方法，统一调用ResetGameState + StartGameplay）
    /// Restart保留玩家的最远路径，影子会播放历史路径
    /// 实现方式：重置各组件状态，而非销毁重建
    /// </summary>
    public void RestartGame()
    {
        GameLogger.Log("重新开始游戏（保留最远路径）", "GameManager");
        
        // 重置状态
        ResetGameState();
        
        // 启动游戏
        StartGameplay();
    }

    /// <summary>
    /// 结束游戏，返回主菜单（由脚本演出调用）
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
            // EndGame时完全重置Boss的最远进度记录
            bossController.ResetMaxSeenProgress();
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

    /// <summary> ----------------------------------------- 脚本演出回调接口（由Animation Event调用） ----------------------------------------- </summary>
    /// <summary>
    /// 入场动画播放完成的回调
    /// 此方法由Animation Event调用，在入场动画的最后一帧触发
    /// 显示血条并开始播放开场脚本演出
    /// </summary>
    public void OnOpeningAnimationComplete()
    {
        GameLogger.Log("入场动画播放完成（通过Animation Event触发），准备播放开场脚本演出", "GameManager");
        
        // 显示血条
        SetUIActive(playerHPBar, true);
        SetUIActive(bossHPBar, true);
        
        // 开始播放开场脚本演出（Player和Boss移动到中央）
        if (cutsceneManager != null)
        {
            cutsceneManager.PlayOpeningCutscene();
        }
        else
        {
            GameLogger.LogWarning("CutsceneManager未赋值，跳过开场演出，直接开始游戏", "GameManager");
            // 如果没有CutsceneManager，直接开始游戏
            OnOpeningCutsceneComplete();
        }
    }
    
    /// <summary>
    /// 开场脚本演出完成的回调
    /// 此方法由CutsceneManager调用，在开场演出的最后一帧触发
    /// 启动游戏序列（状态已在dieAndRestart动画中重置）
    /// </summary>
    public void OnOpeningCutsceneComplete()
    {
        GameLogger.Log("开场脚本演出完成，启动游戏序列", "GameManager");
        
        // 启动游戏序列（Boss攻击、玩家输入、影子系统）
        StartGameplay();
    }
    
    /// <summary>
    /// 比赛开始脚本演出完成的回调
    /// 此方法由CutsceneManager调用
    /// </summary>
    public void OnGameStartCutsceneComplete()
    {
        GameLogger.Log("比赛开始脚本演出完成", "GameManager");
        // 可以在这里添加额外逻辑
    }
    
    /// <summary>
    /// 重新开始脚本演出完成的回调
    /// 此方法由CutsceneManager调用，在重新开始演出的最后一帧触发
    /// 执行重新开始游戏逻辑
    /// </summary>
    public void OnRestartCutsceneComplete()
    {
        GameLogger.Log("重新开始脚本演出完成，执行重新开始逻辑", "GameManager");
        
        // 执行重新开始逻辑
        RestartGame();
    }
    
    /// <summary>
    /// 胜利脚本演出完成的回调
    /// 此方法由CutsceneManager调用，在胜利演出的最后一帧触发
    /// 可以在这里添加胜利后的逻辑（如返回主菜单或下一关卡）
    /// </summary>
    public void OnVictoryCutsceneComplete()
    {
        GameLogger.Log("胜利脚本演出完成", "GameManager");
        // 可以在这里添加胜利后的逻辑，比如返回主菜单
        // EndGame();
    }
    
    /// <summary>
    /// 失败脚本演出完成的回调
    /// 此方法由CutsceneManager调用，在失败演出的最后一帧触发
    /// 可以在这里选择重新开始或返回主菜单
    /// </summary>
    public void OnDefeatCutsceneComplete()
    {
        GameLogger.Log("失败脚本演出完成", "GameManager");
        // 可以在这里选择自动重新开始或等待玩家选择
        // RestartGame();
    }

    /// <summary> ----------------------------------------- 私有方法 ----------------------------------------- </summary>
    /// <summary>
    /// 订阅UI事件
    /// </summary>
    void SubscribeUIEvents()
    {
        if (uiManager != null)
        {
            // 订阅Beginning动画完成事件
            uiManager.onBeginningComplete.AddListener(OnBeginningUIAnimationComplete);
            // 订阅DieAndRestart动画完成事件
            uiManager.onDieAndRestartComplete.AddListener(OnDieAndRestartUIAnimationComplete);
            GameLogger.Log("已订阅UIManager的动画完成事件", "GameManager");
        }
        else
        {
            GameLogger.LogWarning("UIManager未赋值，无法订阅动画完成事件", "GameManager");
        }
    }
    
    /// <summary>
    /// 取消订阅UI事件
    /// </summary>
    void UnsubscribeUIEvents()
    {
        if (uiManager != null)
        {
            // 取消订阅Beginning动画完成事件
            uiManager.onBeginningComplete.RemoveListener(OnBeginningUIAnimationComplete);
            // 取消订阅DieAndRestart动画完成事件
            uiManager.onDieAndRestartComplete.RemoveListener(OnDieAndRestartUIAnimationComplete);
            GameLogger.Log("已取消订阅UIManager的动画完成事件", "GameManager");
        }
    }
    
    /// <summary>
    /// Beginning UI动画完成回调（由UIManager的UnityEvent触发）
    /// 此方法在Beginning动画完成后被调用，然后触发开场脚本演出
    /// </summary>
    void OnBeginningUIAnimationComplete()
    {
        GameLogger.Log("Beginning UI动画完成，重置游戏状态并播放开场脚本演出", "GameManager");
        
        // 显示血条
        SetUIActive(playerHPBar, true);
        SetUIActive(bossHPBar, true);
        
        // 重置游戏状态（恢复生命值等）
        ResetGameState();
        
        // 开始播放开场脚本演出（Player和Boss移动到中央）
        PlayOpeningCutsceneSequence();
    }
    
    /// <summary>
    /// DieAndRestart UI动画完成回调（由UIManager的UnityEvent触发）
    /// 此方法在DieAndRestart动画完成后被调用，然后触发开场脚本演出
    /// </summary>
    void OnDieAndRestartUIAnimationComplete()
    {
        GameLogger.Log("DieAndRestart UI动画完成，准备播放开场脚本演出", "GameManager");
        
        // 显示血条（确保血条可见）
        SetUIActive(playerHPBar, true);
        SetUIActive(bossHPBar, true);
        
        // 开始播放开场脚本演出（Player和Boss移动到中央）
        PlayOpeningCutsceneSequence();
    }
    
    /// <summary>
    /// 播放开场脚本演出序列（统一方法）
    /// </summary>
    void PlayOpeningCutsceneSequence()
    {
        // 开始播放开场脚本演出（Player和Boss移动到中央）
        if (cutsceneManager != null)
        {
            cutsceneManager.PlayOpeningCutscene();
        }
        else
        {
            GameLogger.LogWarning("CutsceneManager未赋值，跳过开场演出，直接重新开始游戏", "GameManager");
            // 如果没有CutsceneManager，直接重新开始游戏
            OnOpeningCutsceneComplete();
        }
    }
    
    /// <summary>
    /// 初始化UI状态（在Awake中调用，确保启动时UI正确）
    /// 初始时隐藏所有UI，等待入场动画播放完成后再显示
    /// </summary>
    void InitializeUIState()
    {
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
        
        // 播放胜利脚本演出
        if (cutsceneManager != null)
        {
            cutsceneManager.PlayVictoryCutscene();
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
        
        // 注意：失败演出由OnPlayerDeath()中调用的dieAndRestart动画处理
        // 不在这里播放失败脚本演出
        
        // 可以在这里添加：
        // - 播放失败音效
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
