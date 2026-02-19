using UnityEngine;

[DisallowMultipleComponent]
public class RopePendulumAdapter : MonoBehaviour
{
    [Header("References")]
    public VerletRope7 rope;
    public Rigidbody2D bob;
    public Transform anchor;

    [Header("Behavior profile (not physics source)")]
    public PendulumBehaviorProfile pendulumBehavior;

    private void Reset()
    {
        // Reset runs in editor when component is first added.
        // Try to auto-assign a local co-located rope so the component is usable immediately.
        if (rope == null)
            rope = GetComponent<VerletRope7>();
    }

    private void Awake()
    {
        // One-time auto-resolution if user left rope null and option enabled.
        if (rope == null)
        {
            var local = GetComponent<VerletRope7>();
            if (local != null)
            {
                rope = local;
#if UNITY_EDITOR
                Debug.Log($"{name}: Auto-resolved local VerletRope7 via GetComponent().", this);
#endif
            }
        }

    }

    public bool IsReady => rope != null && rope.IsReady && rope.isTaut && bob != null && pendulumBehavior != null;

    public Transform Anchor => anchor != null ? anchor : (rope != null ? rope.startPoint : null);
    public Rigidbody2D Bob => bob;
    public float IdealLength => rope != null ? rope.ComputeIdealLength() : 0f;
    public RopePhysicsProfile RopePhysics => rope != null && rope.profile != null ? rope.profile.physics : null;
}