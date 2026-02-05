using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    [Header("UI Animations")]
    [SerializeField] private Animation openingAnimation;
    [SerializeField] private Animation beginningAnimation;
    [SerializeField] private Animation dieAndRestartAnimation;
    [SerializeField] private Animation winAnimation;
    
    [Header("Animation Settings")]
    [SerializeField] private string openingAnimationName = "OpeningAnimation";
    [SerializeField] private string beginningAnimationName = "BeginningAnimation";
    [SerializeField] private string dieAndRestartAnimationName = "dieAndRestart";
    [SerializeField] private string winAnimationName = "WinAnimation";
    
    [Header("Wait Time Settings")]
    [SerializeField] private float waitBeforeOpening = 0f;
    [SerializeField] private float waitAfterOpening = 0f;
    [SerializeField] private float waitBeforeBeginning = 0f;
    [SerializeField] private float waitAfterBeginning = 0f;
    [SerializeField] private float waitBeforeDieAndRestart = 0f;
    [SerializeField] private float waitAfterDieAndRestart = 0f;
    [SerializeField] private float waitBeforeWin = 0f;
    [SerializeField] private float waitAfterWin = 0f;

    [Header("Events")]
    [SerializeField] public UnityEvent onOpeningComplete;
    [SerializeField] public UnityEvent onBeginningComplete;  // Beginning动画完成事件
    [SerializeField] public UnityEvent onDieAndRestartComplete;
    [SerializeField] public UnityEvent onWinComplete;
    
    private bool isFirstLaunch = true;
    
    private void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // 游戏第一次启动时播放动画序列
        if (isFirstLaunch)
        {
            PlayStartupSequence();
        }
    }

    private void Update()
    {
        // 检测F5键按下
        if (Input.GetKeyDown(KeyCode.F5))
        {
            PlayDieAndRestartAnimation();
        }
        
        // 检测F6键按下
        if (Input.GetKeyDown(KeyCode.F6))
        {
            PlayWinAnimation();
        }
    }
    
    /// <summary>
    /// 播放启动动画序列（Opening -> Beginning）
    /// </summary>
    public void PlayStartupSequence()
    {
        StartCoroutine(StartupSequenceCoroutine());
    }
    
    /// <summary>
    /// 播放Opening动画（工作室LOGO）
    /// </summary>
    public void PlayOpeningAnimation()
    {
        if (openingAnimation != null)
        {
            openingAnimation.Play(openingAnimationName);
        }
    }
    
    /// <summary>
    /// 播放Beginning动画（游戏开始UI）
    /// </summary>
    public void PlayBeginningAnimation()
    {
        if (beginningAnimation != null)
        {
            beginningAnimation.Play(beginningAnimationName);
        }
    }
    
    /// <summary>
    /// Beginning动画完成回调（由Animation Event调用）
    /// 此方法可以在Beginning动画的最后一帧通过Animation Event调用
    /// </summary>
    public void OnBeginningAnimationComplete()
    {
        // 触发Beginning完成事件，通知所有监听者（如GameManager）
        onBeginningComplete?.Invoke();
    }
    
    /// <summary>
    /// DieAndRestart动画完成回调（由Animation Event调用）
    /// 此方法可以在DieAndRestart动画的最后一帧通过Animation Event调用
    /// </summary>
    public void OnDieAndRestartAnimationComplete()
    {
        // 触发DieAndRestart完成事件，通知所有监听者（如GameManager）
        onDieAndRestartComplete?.Invoke();
    }
    
    /// <summary>
    /// DieAndRestart动画中间帧回调（由Animation Event调用）
    /// 此方法在DieAndRestart动画播放到中间某一帧时调用，用于通知GameManager重置游戏状态
    /// 建议在动画播放到一半左右的时候调用，让玩家和Boss的状态在视觉上合理的时机重置
    /// </summary>
    public void OnResetGameState()
    {
        // 通知GameManager重置游戏状态
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetGameState();
        }
    }

    /// <summary>
    /// 播放dieAndRestart动画
    /// </summary>
    public void PlayDieAndRestartAnimation()
    {
        StartCoroutine(DieAndRestartCoroutine());
    }
    
    /// <summary>
    /// 播放Win动画
    /// </summary>
    public void PlayWinAnimation()
    {
        StartCoroutine(WinCoroutine());
    }
    
    private IEnumerator StartupSequenceCoroutine()
    {
        // 等待Opening动画前的等待时间
        yield return new WaitForSeconds(waitBeforeOpening);
        
        // 播放Opening动画
        PlayOpeningAnimation();
        
        // 等待Opening动画播放完毕
        if (openingAnimation != null)
        {
            yield return new WaitForSeconds(GetAnimationLength(openingAnimation, openingAnimationName));
        }
        
        // 触发Opening完成事件
        onOpeningComplete?.Invoke();
        
        // 等待Opening动画后的等待时间
        yield return new WaitForSeconds(waitAfterOpening);
        
        // 等待Beginning动画前的等待时间
        yield return new WaitForSeconds(waitBeforeBeginning);
        
        // 播放Beginning动画
        PlayBeginningAnimation();
        
        // 注意：Beginning动画完成事件由Animation Event触发OnBeginningAnimationComplete()
        // 不在协程中自动触发，以便在动画的任意帧灵活控制触发时机
        
        // 标记不再是第一次启动
        isFirstLaunch = false;
    }
    
    private IEnumerator DieAndRestartCoroutine()
    {
        // 等待dieAndRestart动画前的等待时间
        yield return new WaitForSeconds(waitBeforeDieAndRestart);
        
        // 播放dieAndRestart动画
        if (dieAndRestartAnimation != null)
        {
            dieAndRestartAnimation.Play(dieAndRestartAnimationName);
            
            // 等待动画播放完毕
            yield return new WaitForSeconds(GetAnimationLength(dieAndRestartAnimation, dieAndRestartAnimationName));
        }
        
        // 等待dieAndRestart动画后的等待时间
        yield return new WaitForSeconds(waitAfterDieAndRestart);
        
        // 触发DieAndRestart完成事件
        onDieAndRestartComplete?.Invoke();
    }
    
    private IEnumerator WinCoroutine()
    {
        // 等待Win动画前的等待时间
        yield return new WaitForSeconds(waitBeforeWin);
        
        // 播放Win动画
        if (winAnimation != null)
        {
            winAnimation.Play(winAnimationName);
            
            // 等待动画播放完毕
            yield return new WaitForSeconds(GetAnimationLength(winAnimation, winAnimationName));
        }
        
        // 等待Win动画后的等待时间
        yield return new WaitForSeconds(waitAfterWin);
        
        // 触发Win完成事件
        onWinComplete?.Invoke();
    }
    
    private float GetAnimationLength(Animation animation, string animationName)
    {
        if (animation != null && animation[animationName] != null)
        {
            return animation[animationName].length;
        }
        return 0f;
    }
}