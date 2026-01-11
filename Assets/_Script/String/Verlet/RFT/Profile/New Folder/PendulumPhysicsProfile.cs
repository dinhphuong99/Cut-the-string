using UnityEngine;

[CreateAssetMenu(fileName = "PendulumPhysicsProfile", menuName = "Physics Profiles/Pendulum Physics Profile")]
public class PendulumPhysicsProfile : ScriptableObject
{
    [Header("Spring (Hooke)")]
    [Tooltip("Spring constant k (N/m).")]
    public float k = 50f;

    [Tooltip("Max allowed stretch factor relative to rest length (>=1).")]
    public float maxStretchFactor = 1.3f;

    [Header("Damping (Directional along rope)")]
    [Tooltip("If true, compute c = 2 * sqrt(k * m) * dampingZeta")]
    public bool useCriticalDamping = true;

    [Tooltip("Damping ratio ζ (0..2). 1 = critical. 0.2-0.6 reasonable for pendulum feel).")]
    [Range(0f, 2f)]
    public float dampingZeta = 0.3f;

    [Tooltip("If not using critical damping, use this fixed c (F = -c * v_along).")]
    public float dampingC_Fixed = 2f;

    [Header("Air drag (isotropic)")]
    [Tooltip("Small viscous air drag applied always: F_air = -airDragC * v")]
    public float airDragC = 0.05f;

    [Header("Behavior")]
    [Tooltip("If true, spring is bidirectional (push when compressed). If false, rope only pulls.")]
    public bool bidirectionalElasticity = false;

    [Tooltip("Physics substeps per FixedUpdate used by module for better stability.")]
    [Min(1)]
    public int substeps = 1;

#if UNITY_EDITOR
    private void OnValidate()
    {
        k = Mathf.Max(0f, k);
        maxStretchFactor = Mathf.Max(1f, maxStretchFactor);
        dampingC_Fixed = Mathf.Max(0f, dampingC_Fixed);
        airDragC = Mathf.Max(0f, airDragC);
        substeps = Mathf.Max(1, substeps);
    }
#endif
}