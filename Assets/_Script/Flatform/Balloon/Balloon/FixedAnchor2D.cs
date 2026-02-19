using UnityEngine;

/// <summary>
/// FixedAnchor2D là "điểm neo" để Balloon attach vào.
/// Chỉ cần Transform là đủ, script này giúp bạn có gizmo nhìn rõ.
/// </summary>
public class FixedAnchor2D : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] private float gizmoSize = 0.15f;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, gizmoSize);
    }
#endif
}