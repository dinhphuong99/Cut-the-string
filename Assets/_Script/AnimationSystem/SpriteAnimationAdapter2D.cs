// Assets/Scripts/AnimationSystem/Runtime/SpriteAnimationAdapter2D.cs
using UnityEngine;

[DisallowMultipleComponent]
public class SpriteAnimationAdapter2D : MonoBehaviour, IAnimationAdapter
{
    [SerializeField] private Animator animator;
    public RuntimeAnimatorController baseController;
    public string placeholderClipName = "PLACEHOLDER_BASE";
    public string fullBodyStateName = "BaseSlot";
    public string idleStateName = "Idle";

    private AnimatorOverrideController _overrideController;

    void Reset() => animator = GetComponentInChildren<Animator>();

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null && baseController != null)
        {
            _overrideController = new AnimatorOverrideController(baseController);
            animator.runtimeAnimatorController = _overrideController;
        }
    }

    public void PlayClip(AnimationClip clip, float playbackSpeed, float crossfadeSeconds, bool upperBody = false)
    {
        if (animator == null || _overrideController == null || clip == null) return;
        _overrideController[placeholderClipName] = clip;
        animator.speed = Mathf.Max(0.0001f, playbackSpeed);
        animator.CrossFadeInFixedTime(fullBodyStateName, crossfadeSeconds, 0);
    }

    public void HardStop()
    {
        if (animator == null) return;
        animator.Play(idleStateName, 0, 0f);
        animator.speed = 1f;
    }

    public void UpdatePlaybackSpeed(float playbackSpeed)
    {
        if (animator == null) return;
        animator.speed = Mathf.Max(0.0001f, playbackSpeed);
    }
}