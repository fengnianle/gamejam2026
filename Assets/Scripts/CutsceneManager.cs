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
    
    [Tooltip("可选：Player Shadow对象（用于演出中同步控制Shadow的Animator）")]
    public GameObject playerShadowObject;
    
    [Tooltip("可选：Boss Shadow对象（用于演出中同步控制Shadow的Animator）")]
    public GameObject bossShadowObject;

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
    [Tooltip("初始位置动画的状态名称（单帧动画，用于重置Player和Boss到初始位置，距离远）")]
    public string initialPositionState = "InitialPosition";
    
    [Tooltip("开场演出动画的状态名称")]
    public string openingCutsceneState = "OpeningCutscene";
    
    [Tooltip("角色弹开动画的状态名称（攻击模式结束时播放）")]
    public string roundGapState = "RoundGap";
    
    [Tooltip("获胜演出动画的状态名称（玩家获胜时播放）")]
    public string winState = "Win";
    
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
    
    [Tooltip("演出开始前的相机渲染层（用于演出结束后恢复）")]
    private LayerMask previousCullingMask;

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
        
        // 禁用Player和Boss的Animator，避免角色自身动画干扰演出
        DisableCharacterAnimators();
        
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
        
        // 禁用Player和Boss的Animator，避免角色自身动画干扰演出
        DisableCharacterAnimators();
        
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
        
        // 禁用Player和Boss的Animator，避免角色自身动画干扰演出
        DisableCharacterAnimators();
        
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
        
        // 禁用Player和Boss的Animator，避免角色自身动画干扰演出
        DisableCharacterAnimators();
        
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
        
        // 禁用Player和Boss的Animator，避免角色自身动画干扰演出
        DisableCharacterAnimators();
        
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

    /// <summary>
    /// 播放获胜演出（玩家获胜时调用）
    /// </summary>
    public void PlayWinCutscene()
    {
        GameLogger.Log("CutsceneManager: 开始播放获胜演出", "CutsceneManager");
        
        // 禁用Player和Boss的Animator，避免角色自身动画干扰演出
        DisableCharacterAnimators();
        
        if (cutsceneAnimator != null)
        {
            isPlayingCutscene = true;
            cutsceneAnimator.Play(winState);
        }
        else
        {
            // 如果没有动画，直接通知获胜演出完成
            OnWinCutsceneComplete();
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
        
        // 重新启用Player和Boss的Animator
        EnableCharacterAnimators();
        
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
        
        // 【关键】确保角色Animator已启用（防止演出中被禁用）
        EnableCharacterAnimators();
        
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
        
        // 重新启用Player和Boss的Animator
        EnableCharacterAnimators();
        
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
        
        // 重新启用Player和Boss的Animator
        EnableCharacterAnimators();
        
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
        
        // 重新启用Player和Boss的Animator
        EnableCharacterAnimators();
        
        // 通知GameManager失败演出已完成
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDefeatCutsceneComplete();
        }
    }

    /// <summary>
    /// 获胜演出完成回调（由Animation Event调用）
    /// </summary>
    public void OnWinCutsceneComplete()
    {
        GameLogger.Log("CutsceneManager: 获胜演出完成（通过Animation Event触发）", "CutsceneManager");
        
        isPlayingCutscene = false;
        
        // 重新启用Player和Boss的Animator
        EnableCharacterAnimators();
        
        // 通知GameManager获胜演出已完成
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnWinCutsceneComplete();
        }
    }

    /// <summary> ----------------------------------------- 音频控制接口（调用AudioManager） ----------------------------------------- </summary>
    
    /// <summary>
    /// 播放DashIn音效
    /// </summary>
    public void PlayDashInSFX()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDashIn();
            GameLogger.Log("CutsceneManager: 已播放DashIn音效", "CutsceneManager");
        }
        else
        {
            GameLogger.LogWarning("CutsceneManager: AudioManager.Instance为null，无法播放DashIn音效！", "CutsceneManager");
        }
    }
    
    /// <summary>
    /// 播放DashOut音效
    /// </summary>
    public void PlayDashOutSFX()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDashOut();
            GameLogger.Log("CutsceneManager: 已播放DashOut音效", "CutsceneManager");
        }
        else
        {
            GameLogger.LogWarning("CutsceneManager: AudioManager.Instance为null，无法播放DashOut音效！", "CutsceneManager");
        }
    }
    
    /// <summary>
    /// 播放武士冲刺音效（向后兼容，实际调用DashIn）
    /// </summary>
    public void PlaySamuraiDashSFX()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySamuraiDash();
            GameLogger.Log("CutsceneManager: 已播放武士冲刺音效（DashIn）", "CutsceneManager");
        }
        else
        {
            GameLogger.LogWarning("CutsceneManager: AudioManager.Instance为null，无法播放武士冲刺音效！", "CutsceneManager");
        }
    }
    
    /// <summary>
    /// 播放Logo出现音效
    /// </summary>
    public void PlayLogoAppearSFX()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayLogoAppear();
            GameLogger.Log("CutsceneManager: 已播放Logo出现音效", "CutsceneManager");
        }
        else
        {
            GameLogger.LogWarning("CutsceneManager: AudioManager.Instance为null，无法播放Logo出现音效！", "CutsceneManager");
        }
    }
    
    /// <summary>
    /// 播放鼓点音效
    /// </summary>
    public void PlayDrumHitSFX()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDrumHit();
            GameLogger.Log("CutsceneManager: 已播放鼓点音效", "CutsceneManager");
        }
        else
        {
            GameLogger.LogWarning("CutsceneManager: AudioManager.Instance为null，无法播放鼓点音效！", "CutsceneManager");
        }
    }
    
    /// <summary>
    /// 播放剑击音效1
    /// </summary>
    public void PlaySwordClash1SFX()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySwordClash1();
            GameLogger.Log("CutsceneManager: 已播放剑击音效1", "CutsceneManager");
        }
        else
        {
            GameLogger.LogWarning("CutsceneManager: AudioManager.Instance为null，无法播放剑击音效1！", "CutsceneManager");
        }
    }
    
    /// <summary>
    /// 播放剑击音效2
    /// </summary>
    public void PlaySwordClash2SFX()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySwordClash2();
            GameLogger.Log("CutsceneManager: 已播放剑击音效2", "CutsceneManager");
        }
        else
        {
            GameLogger.LogWarning("CutsceneManager: AudioManager.Instance为null，无法播放剑击音效2！", "CutsceneManager");
        }
    }
    
    /// <summary>
    /// 播放剑击音效3
    /// </summary>
    public void PlaySwordClash3SFX()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySwordClash3();
            GameLogger.Log("CutsceneManager: 已播放剑击音效3", "CutsceneManager");
        }
        else
        {
            GameLogger.LogWarning("CutsceneManager: AudioManager.Instance为null，无法播放剑击音效3！", "CutsceneManager");
        }
    }
    
    /// <summary>
    /// 播放随机剑击音效
    /// </summary>
    public void PlayRandomSwordClashSFX()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayRandomSwordClash();
            GameLogger.Log("CutsceneManager: 已播放随机剑击音效", "CutsceneManager");
        }
        else
        {
            GameLogger.LogWarning("CutsceneManager: AudioManager.Instance为null，无法播放随机剑击音效！", "CutsceneManager");
        }
    }
    
    /// <summary>
    /// 播放日式鼓声音效
    /// </summary>
    public void PlayJapaneseDrumSFX()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayJapaneseDrum();
            GameLogger.Log("CutsceneManager: 已播放日式鼓声音效", "CutsceneManager");
        }
        else
        {
            GameLogger.LogWarning("CutsceneManager: AudioManager.Instance为null，无法播放日式鼓声音效！", "CutsceneManager");
        }
    }
    
    /// <summary>
    /// 播放重鼓音效
    /// </summary>
    public void PlayHeavyDrumSFX()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayHeavyDrum();
            GameLogger.Log("CutsceneManager: 已播放重鼓音效", "CutsceneManager");
        }
        else
        {
            GameLogger.LogWarning("CutsceneManager: AudioManager.Instance为null，无法播放重鼓音效！", "CutsceneManager");
        }
    }
    
    /// <summary>
    /// 播放受伤音效
    /// </summary>
    public void PlayHurtSFX()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayHurt();
            GameLogger.Log("CutsceneManager: 已播放受伤音效", "CutsceneManager");
        }
        else
        {
            GameLogger.LogWarning("CutsceneManager: AudioManager.Instance为null，无法播放受伤音效！", "CutsceneManager");
        }
    }
    
    /// <summary>
    /// 播放拔剑音效
    /// </summary>
    public void PlaySwordUnsheathSFX()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySwordUnsheath();
            GameLogger.Log("CutsceneManager: 已播放拔剑音效", "CutsceneManager");
        }
        else
        {
            GameLogger.LogWarning("CutsceneManager: AudioManager.Instance为null，无法播放拔剑音效！", "CutsceneManager");
        }
    }

    /// <summary> ----------------------------------------- 辅助方法 ----------------------------------------- </summary>
    /// <summary>
    /// 禁用角色Animator（在播放演出时避免角色自身动画干扰）
    /// </summary>
    void DisableCharacterAnimators()
    {
        // 【关键】记录当前相机渲染层，并切换到实物层（Player + Boss）
        if (GameManager.Instance != null && GameManager.Instance.shadowCamera != null)
        {
            // 记录当前的 cullingMask
            previousCullingMask = GameManager.Instance.shadowCamera.cullingMask;
            // 切换到实物层
            GameManager.Instance.shadowCamera.cullingMask = GameManager.Instance.defaultLayer;
            GameLogger.Log($"CutsceneManager: 已记录并切换相机渲染层 (之前: {previousCullingMask.value}, 现在: {GameManager.Instance.defaultLayer.value})", "CutsceneManager");
        }
        
        // 禁用Player Animator
        if (playerObject != null)
        {
            Animator playerAnimator = playerObject.GetComponent<Animator>();
            if (playerAnimator != null)
            {
                playerAnimator.enabled = false;
                GameLogger.Log("CutsceneManager: 已禁用Player Animator", "CutsceneManager");
            }
        }
        
        // 禁用Boss Animator
        if (bossObject != null)
        {
            Animator bossAnimator = bossObject.GetComponent<Animator>();
            if (bossAnimator != null)
            {
                bossAnimator.enabled = false;
                GameLogger.Log("CutsceneManager: 已禁用Boss Animator", "CutsceneManager");
            }
        }
        
        // 禁用Player Shadow Animator
        if (playerShadowObject != null)
        {
            Animator playerShadowAnimator = playerShadowObject.GetComponent<Animator>();
            if (playerShadowAnimator != null)
            {
                playerShadowAnimator.enabled = false;
                GameLogger.Log("CutsceneManager: 已禁用Player Shadow Animator", "CutsceneManager");
            }
        }
        
        // 禁用Boss Shadow Animator
        if (bossShadowObject != null)
        {
            Animator bossShadowAnimator = bossShadowObject.GetComponent<Animator>();
            if (bossShadowAnimator != null)
            {
                bossShadowAnimator.enabled = false;
                GameLogger.Log("CutsceneManager: 已禁用Boss Shadow Animator", "CutsceneManager");
            }
        }
    }
    
    /// <summary>
    /// 启用角色Animator（演出结束后恢复角色动画）
    /// </summary>
    void EnableCharacterAnimators()
    {
        // 【关键】恢复之前记录的相机渲染层
        if (GameManager.Instance != null && GameManager.Instance.shadowCamera != null)
        {
            GameManager.Instance.shadowCamera.cullingMask = previousCullingMask;
            GameLogger.Log($"CutsceneManager: 已恢复相机渲染层 (恢复为: {previousCullingMask.value})", "CutsceneManager");
        }
        
        // 启用Player Animator
        if (playerObject != null)
        {
            Animator playerAnimator = playerObject.GetComponent<Animator>();
            if (playerAnimator != null)
            {
                playerAnimator.enabled = true;
                GameLogger.Log("CutsceneManager: 已启用Player Animator", "CutsceneManager");
            }
        }
        
        // 启用Boss Animator
        if (bossObject != null)
        {
            Animator bossAnimator = bossObject.GetComponent<Animator>();
            if (bossAnimator != null)
            {
                bossAnimator.enabled = true;
                GameLogger.Log("CutsceneManager: 已启用Boss Animator", "CutsceneManager");
            }
        }
        
        // 启用Player Shadow Animator
        if (playerShadowObject != null)
        {
            Animator playerShadowAnimator = playerShadowObject.GetComponent<Animator>();
            if (playerShadowAnimator != null)
            {
                playerShadowAnimator.enabled = true;
                GameLogger.Log("CutsceneManager: 已启用Player Shadow Animator", "CutsceneManager");
            }
        }
        
        // 启用Boss Shadow Animator
        if (bossShadowObject != null)
        {
            Animator bossShadowAnimator = bossShadowObject.GetComponent<Animator>();
            if (bossShadowAnimator != null)
            {
                bossShadowAnimator.enabled = true;
                GameLogger.Log("CutsceneManager: 已启用Boss Shadow Animator", "CutsceneManager");
            }
        }
    }
    
    /// <summary>
    /// 重置演出动画控制器到指定状态（内部方法）
    /// 在演出完成后调用，确保动画控制器返回到可重新播放的状态
    /// </summary>
    /// <param name="targetState">目标状态名称，默认为初始位置状态</param>
    void ResetCutsceneAnimator(string targetState = null)
    {
        if (cutsceneAnimator != null)
        {
            // 如果没有指定目标状态，使用初始位置状态
            string stateToPlay = string.IsNullOrEmpty(targetState) ? initialPositionState : targetState;
            cutsceneAnimator.Play(stateToPlay);
            GameLogger.Log($"CutsceneManager: 已重置演出动画控制器到状态: {stateToPlay}", "CutsceneManager");
        }
    }
    
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

	public void PlayHitPause()
	{
		HitPauseManager.Instance.CallHitPause(0.5f,0.5f);
	}

	public void PlayFinalHitPause()
	{
		HitPauseManager.Instance.CallHitPause(0.52f,0.4f);
	}

	public void CameraToMinSize()
	{
		CameraController.Instance.TransitionToMinSize();
	}

	public void CameraToMaxSize()
	{
		CameraController.Instance.TransitionToMaxSize();
	}

	public void CallWinPerformance()
	{
		UIManager.Instance.PlayWinAnimation();
	}

	/// <summary>
	/// 播放双方弹开动画（攻击模式结束时调用）
	/// </summary>
	public void PlayRoundGapAnimation()
	{
		GameLogger.Log("CutsceneManager: 播放双方弹开动画", "CutsceneManager");
		
		// 禁用Player和Boss的Animator，避免角色自身动画干扰演出
		DisableCharacterAnimators();
		
		if (cutsceneAnimator != null)
		{
			// 【调试】输出当前状态信息
			AnimatorStateInfo currentState = cutsceneAnimator.GetCurrentAnimatorStateInfo(0);
			GameLogger.Log($"CutsceneManager: 当前动画状态: {currentState.shortNameHash}, 准备切换到 {roundGapState}", "CutsceneManager");
			
			// 【关键】使用协程延迟一帧播放，确保 Animator 状态机已准备好
			StartCoroutine(PlayRoundGapDelayed());
		}
		else
		{
			GameLogger.LogWarning("CutsceneManager: cutsceneAnimator为null，无法播放双方弹开动画！", "CutsceneManager");
		}
	}
	
	/// <summary>
	/// 延迟播放 RoundGap 动画（协程）
	/// </summary>
	private System.Collections.IEnumerator PlayRoundGapDelayed()
	{
		// 等待下一帧，让 Animator 状态机更新
		yield return null;
		
		// 强制从头播放 RoundGap 动画
		cutsceneAnimator.Play(roundGapState, -1, 0f);
		GameLogger.Log($"CutsceneManager: 已在协程中调用 cutsceneAnimator.Play({roundGapState}, -1, 0f)", "CutsceneManager");
	}
	
	/// <summary>
	/// RoundGap动画完成回调（由Animation Event调用）
	/// </summary>
	public void OnRoundGapAnimationComplete()
	{
		GameLogger.Log("CutsceneManager: RoundGap动画完成（通过Animation Event触发）", "CutsceneManager");
		
		// 重新启用Player和Boss的Animator
		EnableCharacterAnimators();
	}
}
