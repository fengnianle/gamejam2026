using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[System.Serializable]
public enum FlickerMode
{
    Random,         // 随机闪烁
    Pulse,          // 脉冲闪烁
    Wave,           // 波浪闪烁
    Strobe          // 频闪效果
}

public class LightFlicker : MonoBehaviour
{
    [Header("Light Settings")]
    [SerializeField] private Light2D targetLight;
    
    [Header("Flicker Settings")]
    [SerializeField] private FlickerMode flickerMode = FlickerMode.Random;
    [SerializeField] private bool enableFlicker = true;
    
    [Header("Intensity Settings")]
    [SerializeField] private float minIntensity = 0.5f;
    [SerializeField] private float maxIntensity = 1.5f;
    [SerializeField] private float originalIntensity = 1f;
    
    [Header("Speed Settings")]
    [SerializeField] private float flickerSpeed = 2f;
    [SerializeField] private float randomInterval = 0.1f;
    
    [Header("Pulse Settings")]
    [SerializeField] private float pulseDuration = 1f;
    
    [Header("Strobe Settings")]
    [SerializeField] private float strobeDuration = 0.1f;
    [SerializeField] private float strobeInterval = 0.5f;
    
    private float timer = 0f;
    private bool strobeState = true;
    private Coroutine flickerCoroutine;

    void Start()
    {
        // 如果没有指定Light2D组件，尝试获取当前物体上的组件
        if (targetLight == null)
        {
            targetLight = GetComponent<Light2D>();
        }
        
        if (targetLight == null)
        {
            Debug.LogWarning("LightFlicker: 没有找到Light2D组件！请确保物体上有Light2D组件或手动指定targetLight。");
            enabled = false;
            return;
        }
        
        // 保存原始强度
        originalIntensity = targetLight.intensity;
        
        // 开始闪烁
        if (enableFlicker)
        {
            StartFlicker();
        }
    }
    
    void Update()
    {
        if (!enableFlicker || targetLight == null) return;
        
        timer += Time.deltaTime;
        
        switch (flickerMode)
        {
            case FlickerMode.Random:
                UpdateRandomFlicker();
                break;
            case FlickerMode.Pulse:
                UpdatePulseFlicker();
                break;
            case FlickerMode.Wave:
                UpdateWaveFlicker();
                break;
            case FlickerMode.Strobe:
                UpdateStrobeFlicker();
                break;
        }
    }
    
    private void UpdateRandomFlicker()
    {
        if (timer >= randomInterval)
        {
            float randomIntensity = Random.Range(minIntensity, maxIntensity);
            targetLight.intensity = randomIntensity;
            timer = 0f;
            randomInterval = Random.Range(0.05f, 0.2f);
        }
    }
    
    private void UpdatePulseFlicker()
    {
        float normalizedTime = (timer % pulseDuration) / pulseDuration;
        float intensity = Mathf.Lerp(minIntensity, maxIntensity, 
            (Mathf.Sin(normalizedTime * Mathf.PI * 2 * flickerSpeed) + 1) * 0.5f);
        targetLight.intensity = intensity;
    }
    
    private void UpdateWaveFlicker()
    {
        float wave = Mathf.Sin(timer * flickerSpeed * Mathf.PI * 2);
        float intensity = Mathf.Lerp(minIntensity, maxIntensity, (wave + 1) * 0.5f);
        targetLight.intensity = intensity;
    }
    
    private void UpdateStrobeFlicker()
    {
        if (timer >= strobeInterval)
        {
            strobeState = !strobeState;
            targetLight.intensity = strobeState ? maxIntensity : minIntensity;
            timer = 0f;
            
            // 如果是开启状态，使用strobeDuration作为间隔
            if (strobeState)
            {
                strobeInterval = strobeDuration;
            }
            else
            {
                strobeInterval = strobeInterval;
            }
        }
    }
    
    public void StartFlicker()
    {
        enableFlicker = true;
        timer = 0f;
    }
    
    public void StopFlicker()
    {
        enableFlicker = false;
        if (targetLight != null)
        {
            targetLight.intensity = originalIntensity;
        }
    }
    
    public void SetFlickerMode(FlickerMode mode)
    {
        flickerMode = mode;
        timer = 0f;
    }
    
    public void SetIntensityRange(float min, float max)
    {
        minIntensity = min;
        maxIntensity = max;
    }
    
    public void SetFlickerSpeed(float speed)
    {
        flickerSpeed = speed;
    }
    
    // 重置到原始强度
    public void ResetToOriginalIntensity()
    {
        if (targetLight != null)
        {
            targetLight.intensity = originalIntensity;
        }
    }
    
    void OnDisable()
    {
        StopFlicker();
    }
    
    void OnValidate()
    {
        // 确保最小强度不大于最大强度
        if (minIntensity > maxIntensity)
        {
            minIntensity = maxIntensity;
        }
        
        // 确保参数在合理范围内
        flickerSpeed = Mathf.Max(0.1f, flickerSpeed);
        randomInterval = Mathf.Max(0.01f, randomInterval);
        pulseDuration = Mathf.Max(0.1f, pulseDuration);
        strobeDuration = Mathf.Max(0.01f, strobeDuration);
        strobeInterval = Mathf.Max(0.01f, strobeInterval);
    }
}
