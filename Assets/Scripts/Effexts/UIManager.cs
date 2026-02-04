using UnityEngine;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    [Header("UI Animations")]
    [SerializeField] private Animation openingAnimation;
    [SerializeField] private Animation beginningAnimation;
    
    [Header("Animation Settings")]
    [SerializeField] private string openingAnimationName = "OpeningAnimation";
    [SerializeField] private string beginningAnimationName = "BeginningAnimation";
    
    [Header("Wait Time Settings")]
    [SerializeField] private float waitBeforeOpening = 0f;
    [SerializeField] private float waitAfterOpening = 0f;
    [SerializeField] private float waitBeforeBeginning = 0f;
    [SerializeField] private float waitAfterBeginning = 0f;
    
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
        
        // 等待Opening动画后的等待时间
        yield return new WaitForSeconds(waitAfterOpening);
        
        // 等待Beginning动画前的等待时间
        yield return new WaitForSeconds(waitBeforeBeginning);
        
        // 播放Beginning动画
        PlayBeginningAnimation();
        
        // 等待Beginning动画后的等待时间
        yield return new WaitForSeconds(waitAfterBeginning);
        
        // 标记不再是第一次启动
        isFirstLaunch = false;
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