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
            GameLogger.LogComponentValidation($"{controllerName}: 未找到 Animator 组件！请确保已添加Animator组件。", LogType.Error);
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
            GameLogger.LogComponentValidation($"{controllerName}: Idle动画未绑定！", LogType.Warning);
        if (attackXClip == null)
            GameLogger.LogComponentValidation($"{controllerName}: AttackX动画未绑定！", LogType.Warning);
        if (attackYClip == null)
            GameLogger.LogComponentValidation($"{controllerName}: AttackY动画未绑定！", LogType.Warning);
        if (attackBClip == null)
            GameLogger.LogComponentValidation($"{controllerName}: AttackB动画未绑定！", LogType.Warning);
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
            GameLogger.LogComponentValidation($"{controllerName}: {clipName} 动画未绑定！", LogType.Warning);
            return false;
        }
        return true;
    }
}
