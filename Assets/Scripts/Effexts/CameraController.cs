using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float maxSize = 10f;
    [SerializeField] private float minSize = 5f;
    [SerializeField] private float transitionDuration = 1f;
    
    [Header("Camera Shake Settings")]
    [SerializeField] private float shakeIntensity = 1f;
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private AnimationCurve shakeDecayCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    [SerializeField] private bool enableShakeOnHit = true;
    
    [Header("Current State")]
    [SerializeField] private bool isAtMaxSize = true;
    [SerializeField] private bool isShaking = false;
    
    // Singleton instance
    public static CameraController Instance { get; private set; }
    
    private Coroutine transitionCoroutine;
    private Coroutine shakeCoroutine;
    private Vector3 originalPosition;
    
    void Awake()
    {
        // Singleton pattern implementation
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Auto-assign main camera if not set
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
        
        // Store original camera position
        if (targetCamera != null)
        {
            originalPosition = targetCamera.transform.position;
        }
    }
    
    void Start()
    {
        // Initialize camera to max size
        if (targetCamera != null)
        {
            targetCamera.orthographicSize = maxSize;
        }
    }
    
    void Update()
    {
        // Check for F2 key press
        if (Input.GetKeyDown(KeyCode.F2))
        {
            ToggleCameraSize();
        }
        
        // Check for F4 key press to trigger camera shake
        if (Input.GetKeyDown(KeyCode.F4))
        {
            StartCameraShake();
        }
    }
    
    /// <summary>
    /// Toggle between min and max camera sizes
    /// </summary>
    public void ToggleCameraSize()
    {
        if (targetCamera == null) return;
        
        float targetSize = isAtMaxSize ? minSize : maxSize;
        StartCameraSizeTransition(targetSize);
        isAtMaxSize = !isAtMaxSize;
    }
    
    /// <summary>
    /// Transition to max camera size
    /// </summary>
    public void TransitionToMaxSize()
    {
        if (targetCamera == null) return;
        
        StartCameraSizeTransition(maxSize);
        isAtMaxSize = true;
    }
    
    /// <summary>
    /// Transition to min camera size
    /// </summary>
    public void TransitionToMinSize()
    {
        if (targetCamera == null) return;
        
        StartCameraSizeTransition(minSize);
        isAtMaxSize = false;
    }
    
    /// <summary>
    /// Transition to a specific camera size
    /// </summary>
    /// <param name="targetSize">Target orthographic size</param>
    public void TransitionToSize(float targetSize)
    {
        if (targetCamera == null) return;
        
        StartCameraSizeTransition(targetSize);
        isAtMaxSize = Mathf.Approximately(targetSize, maxSize);
    }
    
    /// <summary>
    /// Set transition duration
    /// </summary>
    /// <param name="duration">Duration in seconds</param>
    public void SetTransitionDuration(float duration)
    {
        transitionDuration = Mathf.Max(0f, duration);
    }
    
    /// <summary>
    /// Set max and min sizes
    /// </summary>
    /// <param name="max">Maximum camera size</param>
    /// <param name="min">Minimum camera size</param>
    public void SetSizeRange(float max, float min)
    {
        maxSize = max;
        minSize = min;
    }
    
    /// <summary>
    /// Get current camera size
    /// </summary>
    /// <returns>Current orthographic size</returns>
    public float GetCurrentSize()
    {
        return targetCamera != null ? targetCamera.orthographicSize : 0f;
    }
    
    /// <summary>
    /// Check if camera is currently transitioning
    /// </summary>
    /// <returns>True if transitioning</returns>
    public bool IsTransitioning()
    {
        return transitionCoroutine != null;
    }
    
    private void StartCameraSizeTransition(float targetSize)
    {
        // Stop any existing transition
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
        
        transitionCoroutine = StartCoroutine(TransitionCameraSize(targetSize));
    }
    
    private IEnumerator TransitionCameraSize(float targetSize)
    {
        float startSize = targetCamera.orthographicSize;
        float elapsedTime = 0f;
        
        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionDuration;
            
            // 先慢后快的缓动曲线 (ease-in)
            float easedT = t * t * t; // Cubic ease-in
            targetCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, easedT);
            
            yield return null;
        }
        
        // Ensure we reach the exact target size
        targetCamera.orthographicSize = targetSize;
        transitionCoroutine = null;
    }
    
    /// <summary>
    /// Start camera shake with default parameters
    /// </summary>
    public void StartCameraShake()
    {
        StartCameraShake(shakeIntensity, shakeDuration);
    }
    
    /// <summary>
    /// Start camera shake with custom intensity and duration
    /// </summary>
    /// <param name="intensity">Shake intensity</param>
    /// <param name="duration">Shake duration in seconds</param>
    public void StartCameraShake(float intensity, float duration)
    {
        if (targetCamera == null) return;
        
        // Stop any existing shake
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }
        
        shakeCoroutine = StartCoroutine(CameraShakeCoroutine(intensity, duration));
    }
    
    /// <summary>
    /// Stop camera shake immediately
    /// </summary>
    public void StopCameraShake()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }
        
        // Reset camera position
        if (targetCamera != null)
        {
            Vector3 resetPosition = originalPosition;
            resetPosition.z = targetCamera.transform.position.z; // Preserve Z position
            targetCamera.transform.position = resetPosition;
        }
        
        isShaking = false;
    }
    
    /// <summary>
    /// Set shake parameters
    /// </summary>
    /// <param name="intensity">Default shake intensity</param>
    /// <param name="duration">Default shake duration</param>
    public void SetShakeParameters(float intensity, float duration)
    {
        shakeIntensity = Mathf.Max(0f, intensity);
        shakeDuration = Mathf.Max(0f, duration);
    }
    
    /// <summary>
    /// Set shake decay curve
    /// </summary>
    /// <param name="curve">Animation curve for shake decay</param>
    public void SetShakeDecayCurve(AnimationCurve curve)
    {
        if (curve != null)
        {
            shakeDecayCurve = curve;
        }
    }
    
    /// <summary>
    /// Check if camera is currently shaking
    /// </summary>
    /// <returns>True if shaking</returns>
    public bool IsShaking()
    {
        return isShaking;
    }
    
    /// <summary>
    /// Enable or disable automatic shake on hit
    /// </summary>
    /// <param name="enable">Enable shake on hit</param>
    public void SetShakeOnHit(bool enable)
    {
        enableShakeOnHit = enable;
    }
    
    /// <summary>
    /// Trigger a hit shake (if enabled)
    /// </summary>
    public void OnHit()
    {
        if (enableShakeOnHit)
        {
            StartCameraShake();
        }
    }
    
    /// <summary>
    /// Trigger a hit shake with custom parameters (if enabled)
    /// </summary>
    /// <param name="intensity">Hit shake intensity</param>
    /// <param name="duration">Hit shake duration</param>
    public void OnHit(float intensity, float duration)
    {
        if (enableShakeOnHit)
        {
            StartCameraShake(intensity, duration);
        }
    }
    
    private IEnumerator CameraShakeCoroutine(float intensity, float duration)
    {
        isShaking = true;
        float elapsedTime = 0f;
        Vector3 basePosition = originalPosition;
        basePosition.z = targetCamera.transform.position.z; // Preserve Z position
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            // Apply decay curve
            float decayMultiplier = shakeDecayCurve.Evaluate(t);
            float currentIntensity = intensity * decayMultiplier;
            
            // Generate random offset
            Vector3 randomOffset = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                0f
            ) * currentIntensity;
            
            targetCamera.transform.position = basePosition + randomOffset;
            
            yield return null;
        }
        
        // Reset to original position
        targetCamera.transform.position = basePosition;
        shakeCoroutine = null;
        isShaking = false;
    }
    
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}