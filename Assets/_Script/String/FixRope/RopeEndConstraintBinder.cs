using UnityEngine;

// Gắn script này vào endPoint
[RequireComponent(typeof(MaxDistanceConstraint))]
public class RopeEndConstraintBinder : MonoBehaviour
{
    public RopeFixBehaviour rope;
    private MaxDistanceConstraint constraint;

    void Awake()
    {
        constraint = GetComponent<MaxDistanceConstraint>();
    }

    void Start()
    {
        if (rope == null) return;
        constraint.target = rope.startPoint;
        constraint.maxDistance = rope.segmentLength * (rope.nodeCount - 1);
    }

    void LateUpdate()
    {
        // cập nhật runtime nếu dây co giãn động
        constraint.maxDistance = rope.segmentLength * (rope.nodeCount - 1);
    }
}
