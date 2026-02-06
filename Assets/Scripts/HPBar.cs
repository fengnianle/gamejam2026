using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 血条类型枚举
/// </summary>
public enum HPBarType
{
    Player,
    Boss
}

/// <summary>
/// 血条UI组件
/// 用于显示角色的生命值
/// 使用方法：
/// 1. 挂载到血条的父对象上
/// 2. 在Inspector中拖拽颜色条的Image到fillImage字段
/// 3. 设置血条类型（Player或Boss）
/// 4. 在角色Controller中调用UpdateHP()更新血条显示
/// </summary>
public class HPBar : MonoBehaviour
{
    [Header("血条类型")]
    [Tooltip("标识这个血条属于哪个角色")]
    public HPBarType barType = HPBarType.Player;
    
    [Header("血条UI引用")]
    [Tooltip("血条的填充图片（颜色条）")]
    public Image fillImage;
    
    [Header("血条设置")]
    [Tooltip("血条颜色（正常状态）")]
    public Color normalColor = Color.green;
    
    [Tooltip("血条颜色（低血量状态，低于30%）")]
    public Color lowHealthColor = Color.red;
    
    [Tooltip("血条颜色（中等血量状态，30%-60%）")]
    public Color midHealthColor = Color.yellow;
    
    [Tooltip("低血量阈值（百分比）")]
    [Range(0f, 1f)]
    public float lowHealthThreshold = 0.3f;
    
    [Tooltip("中等血量阈值（百分比）")]
    [Range(0f, 1f)]
    public float midHealthThreshold = 0.6f;

    [Header("视觉修正 (解决边缘毛边问题)")]
    [Tooltip("逻辑血量为0%时，UI实际显示的Fill Amount值")]
    [Range(0f, 1f)]
    public float minFillAmount = 0f;

    [Tooltip("逻辑血量为100%时，UI实际显示的Fill Amount值")]
    [Range(0f, 1f)]
    public float maxFillAmount = 1f;

    void Start()
    {
        // 验证组件
        if (fillImage == null)
        {
            GameLogger.LogError("HPBar: fillImage未绑定！请在Inspector中拖拽颜色条的Image组件。", "HPBar");
            return;
        }

        // 验证Image类型是否为Filled
        if (fillImage.type != Image.Type.Filled)
        {
            GameLogger.LogError($"HPBar: fillImage的Image Type必须设置为Filled！当前类型为 {fillImage.type}。请在Inspector中修改Image Type为Filled。", "HPBar");
            GameLogger.LogError("HPBar: 设置步骤：选中Image对象 -> Inspector -> Image组件 -> Image Type: Filled -> Fill Method: Horizontal", "HPBar");
        }

        // 初始化血条
        fillImage.fillAmount = maxFillAmount;
        fillImage.color = normalColor;
        
        GameLogger.Log($"HPBar初始化: fillAmount = {fillImage.fillAmount}, color = {fillImage.color}", "HPBar");
    }

    /// <summary>
    /// 更新血条显示
    /// </summary>
    /// <param name="currentHP">当前生命值</param>
    /// <param name="maxHP">最大生命值</param>
    public void UpdateHP(float currentHP, float maxHP)
    {
        if (fillImage == null) return;

        // 1. 计算逻辑上的血量百分比 (0.0 - 1.0)
        float logicalHealthPercent = Mathf.Clamp01(currentHP / maxHP);
        
        // 2. 将逻辑百分比映射到视觉FillAmount范围 (minFillAmount - maxFillAmount)
        float visualFillAmount = Mathf.Lerp(minFillAmount, maxFillAmount, logicalHealthPercent);
        
        // 3. 应用视觉值
        fillImage.fillAmount = visualFillAmount;
        
        GameLogger.Log($"HPBar.UpdateHP: currentHP={currentHP}, logicPercent={logicalHealthPercent}, visualFill={visualFillAmount}", "HPBar");

        // 4. 计算颜色 (颜色应该依然根据真实的逻辑百分比来变，而不是视觉百分比)
        UpdateColor(logicalHealthPercent);
    }

    /// <summary>
    /// 直接设置血条（无平滑过渡）
    /// </summary>
    public void SetHP(float currentHP, float maxHP)
    {
        if (fillImage == null) return;

        float logicalHealthPercent = Mathf.Clamp01(currentHP / maxHP);
        float visualFillAmount = Mathf.Lerp(minFillAmount, maxFillAmount, logicalHealthPercent);
        
        fillImage.fillAmount = visualFillAmount;
        
        GameLogger.Log($"HPBar.SetHP: currentHP={currentHP}, visualFill={visualFillAmount}", "HPBar");
        
        UpdateColor(logicalHealthPercent);
    }

    /// <summary>
    /// 根据血量百分比更新颜色
    /// </summary>
    void UpdateColor(float healthPercent)
    {
        if (fillImage == null) return;

        if (healthPercent <= lowHealthThreshold)
        {
            fillImage.color = lowHealthColor;
        }
        else if (healthPercent <= midHealthThreshold)
        {
            fillImage.color = midHealthColor;
        }
        else
        {
            fillImage.color = normalColor;
        }
    }

    /// <summary>
    /// 设置血条颜色
    /// </summary>
    public void SetColor(Color color)
    {
        if (fillImage != null)
        {
            fillImage.color = color;
        }
    }

    /// <summary>
    /// 显示血条
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 隐藏血条
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Unity编辑器验证方法（确保退出Play模式后状态重置）
    /// </summary>
    void OnValidate()
    {
        // 确保在编辑器模式下（非运行时）重置血条状态
        if (!Application.isPlaying && fillImage != null)
        {
            fillImage.fillAmount = maxFillAmount;
            fillImage.color = normalColor;
        }
    }
}
