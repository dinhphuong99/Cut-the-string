// Assets/Scripts/AnimationSystem/Runtime/AnimatorAdapter3D.cs
using UnityEngine;

[DisallowMultipleComponent]
public class AnimatorAdapter3D : MonoBehaviour, IAnimationAdapter
{
    [SerializeField] private Animator animator;
    public RuntimeAnimatorController baseController;
    public string placeholderClipName = "PLACEHOLDER_BASE";
    public string idleStateName = "Idle";
    public string fullBodyStateName = "BaseSlot";
    public string upperBodyStateName = "UpperSlot";
    public int fullBodyLayer = 0;
    public int upperBodyLayer = 1;

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

        if (upperBody)
            animator.CrossFadeInFixedTime(upperBodyStateName, crossfadeSeconds, upperBodyLayer);
        else
            animator.CrossFadeInFixedTime(fullBodyStateName, crossfadeSeconds, fullBodyLayer);
    }

    public void HardStop()
    {
        if (animator == null) return;
        animator.Play(idleStateName, fullBodyLayer, 0f);
        animator.speed = 1f;
    }

    public void UpdatePlaybackSpeed(float playbackSpeed)
    {
        if (animator == null) return;
        animator.speed = Mathf.Max(0.0001f, playbackSpeed);
    }
}