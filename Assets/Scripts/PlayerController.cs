using UnityEngine;

/* 
 * Unity组件绑定说明：
 * 1. 在Hierarchy中选择Player对象
 * 2. 在Inspector中添加以下组件：
 *    - Animator组件（必需）：用于播放动画
 *      - 在Animator组件中设置Animator Controller
 *      - 或者在本脚本中将动画片段拖拽到对应的字段
 * 3. 将本脚本挂载到Player对象上
 * 4. 在本脚本的Inspector面板中：
 *    - 将Idle动画片段拖拽到"Idle Animation"字段
 *    - 将Attack1动画片段拖拽到"Attack1 Animation"字段
 *    - 将Attack2动画片段拖拽到"Attack2 Animation"字段
 *    - 将Attack3动画片段拖拽到"Attack3 Animation"字段
 */

public class PlayerController : MonoBehaviour
{
    [Header("动画绑定")]
    [Tooltip("待机动画片段")]
    public AnimationClip idleAnimation;
    
    [Tooltip("攻击1动画片段（Q键触发）")]
    public AnimationClip attackXAnimation;
    
    [Tooltip("攻击2动画片段（W键触发）")]
    public AnimationClip attackYAnimation;
    
    [Tooltip("攻击3动画片段（E键触发）")]
    public AnimationClip attackBAnimation;

    [Header("组件引用")]
    private Animator animator;

    [Header("状态")]
    private bool isAttacking = false;

    void Start()
    {
        // 获取Animator组件
        animator = GetComponent<Animator>();

        // 验证组件和动画绑定
        ComponentValidator.ValidateAnimator(animator, "PlayerController");
        ComponentValidator.ValidateAnimationClips("PlayerController", 
            idleAnimation, attackXAnimation, attackYAnimation, attackBAnimation);

        // 初始播放Idle动画
        PlayIdleAnimation();
    }

    void Update()
    {
        // 处理攻击输入
        HandleAttackInput();
    }

    /// <summary>
    /// 处理攻击输入
    /// </summary>
    void HandleAttackInput()
    {
       if (isAttacking) return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            PerformAttack(attackXAnimation);
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            PerformAttack(attackYAnimation);
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            PerformAttack(attackBAnimation);
        }
    }

    /// <summary>
    /// 执行攻击
    /// </summary>
    void PerformAttack(AnimationClip attackClip)
    {
        if (!ComponentValidator.CanPlayAnimation(animator, attackClip)) return;

        isAttacking = true;
        animator.Play(attackClip.name);
        Invoke(nameof(ResetAttackState), attackClip.length);
    }

    /// <summary>
    /// 重置攻击状态并返回Idle
    /// </summary>
    void ResetAttackState()
    {
        isAttacking = false;
        PlayIdleAnimation();
    }

    /// <summary>
    /// 播放Idle动画
    /// </summary>
    void PlayIdleAnimation()
    {
        if (ComponentValidator.CanPlayAnimation(animator, idleAnimation))
        {
            animator.Play(idleAnimation.name);
        }
    }

    /// <summary>
    /// 公共方法：停止攻击（可在动画事件中调用）
    /// </summary>
    public void OnAttackComplete()
    {
        isAttacking = false;
    }
}
