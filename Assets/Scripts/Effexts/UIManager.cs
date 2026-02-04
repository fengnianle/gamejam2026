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
    
    [Header("Animation Settings")]
    [SerializeField] private string openingAnimationName = "OpeningAnimation";
    [SerializeField] private string beginningAnimationName = "BeginningAnimation";
    [SerializeField] private string dieAndRestartAnimationName = "dieAndRestart";
    
    [Header("Wait Time Settings")]
    [SerializeField] private float waitBeforeOpening = 0f;
    [SerializeField] private float waitAfterOpening = 0f;
    [SerializeField] private float waitBeforeBeginning = 0f;
    [SerializeField] private float waitAfterBeginning = 0f;
    [SerializeField] private float waitBeforeDieAndRestart = 0f;
    [SerializeField] private float waitAfterDieAndRestart = 0f;

    [Header("Events")]
    [SerializeField] public UnityEvent onOpeningComplete;
    [SerializeField] public UnityEvent onAnimationComplete;
    [SerializeField] public UnityEvent onDieAndRestartComplete;
    
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
    /// 播放dieAndRestart动画
    /// </summary>
    public void PlayDieAndRestartAnimation()
    {
        StartCoroutine(DieAndRestartCoroutine());
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
        
        // 等待Beginning动画后的等待时间
        yield return new WaitForSeconds(waitAfterBeginning);
        
        // 触发Beginning完成事件
        onAnimationComplete?.Invoke();
        
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
    
    private float GetAnimationLength(Animation animation, string animationName)
    {
        if (animation != null && animation[animationName] != null)
        {
            return animation[animationName].length;
        }
        return 0f;
    }
}