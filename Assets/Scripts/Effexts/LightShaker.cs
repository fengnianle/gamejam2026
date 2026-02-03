using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightShaker : MonoBehaviour
{
    [Header("摇摆设置")]
    [SerializeField] private float swaySpeed = 1f; // 摇摆速度
    [SerializeField] private float swayAngle = 5f; // 摇摆角度（度数）
    [SerializeField] private bool enableRandomPhase = true; // 是否启用随机相位
    [SerializeField] private AnimationCurve swayCurve = AnimationCurve.EaseInOut(0, -1, 1, 1); // 摇摆曲线
    [SerializeField] private Vector3 rotationCenterOffset = Vector3.zero; // 旋转中心偏移
    
    [Header("轴向设置")]
    [SerializeField] private bool swayOnX = true; // X轴摇摆
    [SerializeField] private bool swayOnY = false; // Y轴摇摆
    [SerializeField] private bool swayOnZ = false; // Z轴摇摆
    
    [Header("调试")]
    [SerializeField] private bool showDebugGizmos = false; // 显示调试辅助线
    
    private Light2D light2D;
    private Vector3 originalRotation;
    private Vector3 originalPosition;
    private float timeOffset;
    private float lastRotationValue;
    
    void Start()
    {
        // 获取Light2D组件
        light2D = GetComponent<Light2D>();
        if (light2D == null)
        {
            Debug.LogError("LightShaker: 未找到Light2D组件！", this);
            enabled = false;
            return;
        }
        
        // 保存原始旋转和位置
        originalRotation = transform.eulerAngles;
        originalPosition = transform.position;
        
        // 设置随机相位偏移
        if (enableRandomPhase)
        {
            timeOffset = Random.Range(0f, Mathf.PI * 2f);
        }
    }
    
    void Update()
    {
        if (light2D == null) return;
        
        // 计算摇摆值
        float time = Time.time * swaySpeed + timeOffset;
        float swayValue = swayCurve.Evaluate((Mathf.Sin(time) + 1f) * 0.5f);
        swayValue = Mathf.Lerp(-swayAngle, swayAngle, (swayValue + 1f) * 0.5f);
        
        // 如果有旋转中心偏移，围绕偏移点旋转
        if (rotationCenterOffset != Vector3.zero)
        {
            // 计算旋转中心点
            Vector3 pivotPoint = originalPosition + rotationCenterOffset;
            
            // 先重置到原始状态
            transform.position = originalPosition;
            transform.eulerAngles = originalRotation;
            
            // 计算旋转轴
            Vector3 rotationAxis = Vector3.zero;
            if (swayOnX) rotationAxis += Vector3.right;
            if (swayOnY) rotationAxis += Vector3.up;
            if (swayOnZ) rotationAxis += Vector3.forward;
            
            // 围绕指定点旋转
            if (rotationAxis != Vector3.zero)
            {
                transform.RotateAround(pivotPoint, rotationAxis.normalized, swayValue);
            }
        }
        else
        {
            // 计算新的旋转（原有逻辑）
            Vector3 newRotation = originalRotation;
            
            if (swayOnX)
                newRotation.x = originalRotation.x + swayValue;
            if (swayOnY)
                newRotation.y = originalRotation.y + swayValue;
            if (swayOnZ)
                newRotation.z = originalRotation.z + swayValue;
            
            transform.eulerAngles = newRotation;
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;
        
        // 绘制旋转中心偏移点
        if (rotationCenterOffset != Vector3.zero)
        {
            Vector3 pivotPoint = Application.isPlaying ? originalPosition + rotationCenterOffset : transform.position + rotationCenterOffset;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(pivotPoint, 0.1f);
            Gizmos.DrawLine(transform.position, pivotPoint);
        }
        
        // 绘制摇摆范围
        Gizmos.color = Color.yellow;
        Vector3 center = transform.position;
        
        if (swayOnZ) // 最常用的Z轴摇摆
        {
            Vector3 baseRotation = Application.isPlaying ? originalRotation : transform.eulerAngles;
            Vector3 leftDirection = Quaternion.Euler(0, 0, baseRotation.z - swayAngle) * Vector3.up;
            Vector3 rightDirection = Quaternion.Euler(0, 0, baseRotation.z + swayAngle) * Vector3.up;
            
            Gizmos.DrawRay(center, leftDirection * 2f);
            Gizmos.DrawRay(center, rightDirection * 2f);
        }
    }
    
    /// <summary>
    /// 重置到原始位置
    /// </summary>
    [ContextMenu("重置位置")]
    public void ResetToOriginal()
    {
        if (Application.isPlaying)
        {
            transform.position = originalPosition;
            transform.eulerAngles = originalRotation;
        }
    }
    
    /// <summary>
    /// 设置摇摆速度
    /// </summary>
    public void SetSwaySpeed(float speed)
    {
        swaySpeed = Mathf.Max(0f, speed);
    }
    
    /// <summary>
    /// 设置摇摆角度
    /// </summary>
    public void SetSwayAngle(float angle)
    {
        swayAngle = Mathf.Clamp(angle, 0f, 45f);
    }
    
    /// <summary>
    /// 设置旋转中心偏移
    /// </summary>
    public void SetRotationCenterOffset(Vector3 offset)
    {
        rotationCenterOffset = offset;
    }
    
    /// <summary>
    /// 启用/禁用摇摆
    /// </summary>
    public void SetSwayEnabled(bool enabled)
    {
        this.enabled = enabled;
        if (!enabled)
        {
            transform.position = originalPosition;
            transform.eulerAngles = originalRotation;
        }
    }
}