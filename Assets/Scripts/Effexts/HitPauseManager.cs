using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class HitPauseManager : MonoBehaviour
{
    private static HitPauseManager _instance;
    public static HitPauseManager Instance
    {
        get
        {
            if(_instance == null)
            {
                _instance = FindObjectOfType<HitPauseManager>();
            }
            return _instance;
        }
    }

    [Header("Hit Pause Settings")]
    [SerializeField] private float _pauseDuration = 0.1f;  // 暂停持续时间
    [SerializeField] private float _pauseTimeScale = 0.0f;  // 暂停时的时间缩放值
    [SerializeField] private float _resumeSpeed = 1.0f;     // 恢复到正常时间的速度
    
    private Coroutine _hitPauseCoroutine;
    private float _originalTimeScale = 1.0f;

    private void Awake()
    {
        _instance = this;
        _originalTimeScale = Time.timeScale;
    }

    private void Update()
    {
        // 按F3键测试卡肉效果
        if(Keyboard.current.f3Key.wasPressedThisFrame)
        {
            CallHitPause();
        }
    }

    /// <summary>
    /// 触发打击暂停效果
    /// </summary>
    public void CallHitPause()
    {
        CallHitPause(_pauseDuration);
    }

    /// <summary>
    /// 触发打击暂停效果，自定义持续时间
    /// </summary>
    /// <param name="duration">暂停持续时间</param>
    public void CallHitPause(float duration)
    {
        // 如果已经有暂停协程在运行，先停止它
        if(_hitPauseCoroutine != null)
        {
            StopCoroutine(_hitPauseCoroutine);
        }
        
        _hitPauseCoroutine = StartCoroutine(HitPauseAction(duration));
    }

    /// <summary>
    /// 触发打击暂停效果，自定义持续时间和暂停程度
    /// </summary>
    /// <param name="duration">暂停持续时间</param>
    /// <param name="pauseScale">暂停时的时间缩放值</param>
    public void CallHitPause(float duration, float pauseScale)
    {
        if(_hitPauseCoroutine != null)
        {
            StopCoroutine(_hitPauseCoroutine);
        }
        
        _hitPauseCoroutine = StartCoroutine(HitPauseAction(duration, pauseScale));
    }

    private IEnumerator HitPauseAction(float duration)
    {
        return HitPauseAction(duration, _pauseTimeScale);
    }

    private IEnumerator HitPauseAction(float duration, float pauseScale)
    {
        // 立即设置时间缩放为暂停值
        Time.timeScale = pauseScale;

        // 使用非缩放时间等待暂停持续时间
        yield return new WaitForSecondsRealtime(duration);

        // 平滑恢复时间缩放到正常值
        float currentTimeScale = pauseScale;
        while(currentTimeScale < _originalTimeScale)
        {
            currentTimeScale += _resumeSpeed * Time.unscaledDeltaTime;
            currentTimeScale = Mathf.Min(currentTimeScale, _originalTimeScale);
            Time.timeScale = currentTimeScale;
            yield return null;
        }

        // 确保完全恢复到原始时间缩放
        Time.timeScale = _originalTimeScale;
        _hitPauseCoroutine = null;
    }

    /// <summary>
    /// 立即恢复时间缩放到正常值
    /// </summary>
    public void ResumeTime()
    {
        if(_hitPauseCoroutine != null)
        {
            StopCoroutine(_hitPauseCoroutine);
            _hitPauseCoroutine = null;
        }
        Time.timeScale = _originalTimeScale;
    }

    private void OnDestroy()
    {
        // 确保在对象销毁时恢复时间缩放
        Time.timeScale = _originalTimeScale;
    }
}