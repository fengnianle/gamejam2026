using UnityEngine;

/// <summary>
/// 条件隐藏属性
/// 用于根据另一个布尔字段的值来控制字段是否在Inspector中显示
/// </summary>
public class ConditionalHideAttribute : PropertyAttribute
{
    public string ConditionalSourceField { get; private set; }
    public bool HideInInspector { get; private set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="conditionalSourceField">控制显示的布尔字段名称</param>
    /// <param name="hideInInspector">当条件为true时是隐藏(true)还是显示(false)</param>
    public ConditionalHideAttribute(string conditionalSourceField, bool hideInInspector = false)
    {
        ConditionalSourceField = conditionalSourceField;
        HideInInspector = hideInInspector;
    }
}
