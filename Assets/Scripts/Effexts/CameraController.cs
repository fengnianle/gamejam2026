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
    
    [Header("Current State")]
    [SerializeField] private bool isAtMaxSize = true;
    
    // Singleton instance
    public static CameraController Instance { get; private set; }
    
    private Coroutine transitionCoroutine;
    
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
    
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}