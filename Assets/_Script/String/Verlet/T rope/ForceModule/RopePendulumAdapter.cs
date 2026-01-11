using UnityEngine;

[DisallowMultipleComponent]
public class RopePendulumAdapter : MonoBehaviour, IPendulumData
{
    [Header("References")]
    [SerializeField] private VerletRope7 rope;
    [SerializeField] public Rigidbody2D bob;   // public to allow spawner to assign
    [SerializeField] public Transform anchor;  // public to allow spawner to assign

    [Header("Profile")]
    [SerializeField] public PendulumPhysicsProfile pendulumProfile;

    public bool IsReady => rope != null && rope.IsReady && rope.isTaut && bob != null && pendulumProfile != null;

    public Transform Anchor => anchor != null ? anchor : (rope != null ? rope.startPoint : null);

    public Rigidbody2D Bob => bob;

    public float IdealLength => rope != null ? rope.ComputeIdealLength() : 0f;

    public int Substeps => pendulumProfile != null ? Mathf.Max(1, pendulumProfile.substeps) : 1;

    public PendulumPhysicsProfile PendulumPhysics => pendulumProfile;

#if UNITY_EDITOR
    private void Reset()
    {
        if (rope == null) rope = GetComponent<VerletRope7>() ?? GetComponentInParent<VerletRope7>();
        if (anchor == null && rope != null) anchor = rope.startPoint;
        if (bob == null && rope != null && rope.endPoint != null) bob = rope.endPoint.GetComponent<Rigidbody2D>();
    }
#endif
}
