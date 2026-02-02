using UnityEngine;

/// <summary>
/// 组件和资源验证工具类
/// 用于集中处理所有的null检查和验证逻辑
/// </summary>
public static class ComponentValidator
{
    /// <summary>
    /// 验证Animator组件是否存在
    /// </summary>
    public static bool ValidateAnimator(Animator animator, string controllerName)
    {
        if (animator == null)
        {
            Debug.LogError($"{controllerName}: 未找到 Animator 组件！请确保已添加Animator组件。");
            return false;
        }
        return true;
    }

    /// <summary>
    /// 验证动画片段是否已绑定
    /// </summary>
    public static void ValidateAnimationClips(string controllerName, 
        AnimationClip idleClip, 
        AnimationClip attackXClip, 
        AnimationClip attackYClip, 
        AnimationClip attackBClip)
    {
        if (idleClip == null)
            Debug.LogWarning($"{controllerName}: Idle动画未绑定！");
        if (attackXClip == null)
            Debug.LogWarning($"{controllerName}: AttackX动画未绑定！");
        if (attackYClip == null)
            Debug.LogWarning($"{controllerName}: AttackY动画未绑定！");
        if (attackBClip == null)
            Debug.LogWarning($"{controllerName}: AttackB动画未绑定！");
    }

    /// <summary>
    /// 检查动画片段是否可以播放
    /// </summary>
    public static bool CanPlayAnimation(Animator animator, AnimationClip clip)
    {
        return animator != null && clip != null;
    }

    /// <summary>
    /// 检查动画片段并记录警告
    /// </summary>
    public static bool ValidateAndLogClip(AnimationClip clip, string clipName, string controllerName)
    {
        if (clip == null)
        {
            Debug.LogWarning($"{controllerName}: {clipName} 动画未绑定！");
            return false;
        }
        return true;
    }
}
