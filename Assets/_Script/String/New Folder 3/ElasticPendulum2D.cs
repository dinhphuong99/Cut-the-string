using UnityEngine;

/// <summary>
/// ElasticPendulum2D — Force-based elastic pendulum for Rigidbody2D.
/// - Uses Hooke spring (F = -k * x) when rope is stretched (no compression).
/// - Uses directional viscous damping along the rope when taut: F_damp = -c * v_along_rope.
/// - Adds small isotropic air drag: F_air = -c_air * v (always on).
/// - Does NOT mutate gravityScale every FixedUpdate. If gravityScale == 0 in Inspector,
///   script will set it once in Awake and warn.
/// - Tune springK, dampingC and airDragC relative to mass (use critical damping guidance).
/// </summary>
[DisallowMultipleComponent]
public class ElasticPendulum2D : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Anchor transform (fixed or moving).")]
    public Transform anchor;

    [Tooltip("Rigidbody2D of the mass.")]
    public Rigidbody2D massBody;

    [Header("Spring (Hooke)")]
    [Tooltip("Natural (rest) length of the rope/spring.")]
    public float restLength = 2f;

    [Tooltip("Spring constant k (N/m).")]
    public float springK = 40f;

    [Header("Damping (viscous)")]
    [Tooltip("Directional damping coefficient c (N·s/m) applied only along the rope when taut.")]
    public float dampingC = 5f;

    [Tooltip("If true, compute dampingC = autoDampingFactor * c_critical.")]
    public bool useAutoDamping = false;

    [Tooltip("When useAutoDamping=true, multiplier applied to critical damping (1 = critical).")]
    [Range(0f, 3f)]
    public float autoDampingFactor = 0.5f;

    [Header("Air drag (viscous)")]
    [Tooltip("Small isotropic air drag coefficient (N·s/m). Always applied: F_air = -airDragC * v.")]
    public float airDragC = 0.1f;

    [Header("Limits & Safety")]
    [Tooltip("Minimum distance to avoid divide-by-zero.")]
    public float minLength = 0.05f;

    [Tooltip("Maximum allowed stretch relative to restLength. <= 0 means no hard clamp.")]
    public float maxStretch = 0f;

    [Header("Visual (optional)")]
    public LineRenderer lineRenderer;

    // Internal cached mass (for critical damping calculation)
    float bodyMass = 1f;

    void Awake()
    {
        if (massBody == null)
        {
            Debug.LogError("[ElasticPendulum2D] massBody is not assigned.", this);
            enabled = false;
            return;
        }

        if (anchor == null)
        {
            Debug.LogError("[ElasticPendulum2D] anchor is not assigned.", this);
            enabled = false;
            return;
        }

        bodyMass = Mathf.Max(0.0001f, massBody.mass);

        // If Rigidbody2D gravityScale left as 0 in Inspector, set once here to 1
        // (this follows your previous pattern but avoids per-frame side-effects).
        if (Mathf.Approximately(massBody.gravityScale, 0f))
        {
            massBody.gravityScale = 1f;
            Debug.LogWarning("[ElasticPendulum2D] massBody.gravityScale was 0. Set to 1 for simulation. " +
                "Prefer setting gravity scale explicitly in prefab/inspector.", this);
        }

        // If user asked for auto damping, compute dampingC here (depends on mass & k)
        if (useAutoDamping)
        {
            float cCritical = 2f * Mathf.Sqrt(springK * bodyMass); // c_critical = 2 * sqrt(k*m)
            dampingC = cCritical * autoDampingFactor;
            Debug.Log($"[ElasticPendulum2D] Auto damping enabled. c_critical={cCritical:F3}, dampingC set to {dampingC:F3}", this);
        }
    }

    void FixedUpdate()
    {
        if (anchor == null || massBody == null) return;

        Vector2 anchorPos = (Vector2)anchor.position;
        Vector2 massPos = massBody.position;

        Vector2 delta = massPos - anchorPos;
        float currentLength = delta.magnitude;

        // avoid divide by zero
        if (currentLength < minLength)
        {
            // Optional: when extremely close, still apply tiny stabilization force toward anchor to avoid jitter
            return;
        }

        Vector2 dir = delta / currentLength; // unit vector from anchor -> mass

        // Only treat positive extension as spring (rope can't push)
        float extension = Mathf.Max(0f, currentLength - restLength);

        if (maxStretch > 0f)
        {
            extension = Mathf.Clamp(extension, 0f, maxStretch);
        }

        Vector2 totalForce = Vector2.zero;

        // Spring force (Hooke), only when extension > 0
        if (extension > Mathf.Epsilon)
        {
            // F_spring = -k * x  (negative scalar), applied along dir
            float springForceScalar = -springK * extension;
            Vector2 springForce = dir * springForceScalar;

            // Directional damping along the rope (viscous), only when rope taut
            float velAlong = Vector2.Dot(massBody.linearVelocity, dir); // positive when mass moving away from anchor
            float dampingForceScalar = -dampingC * velAlong;
            Vector2 dampingForce = dir * dampingForceScalar;

            totalForce += springForce + dampingForce;
        }
        // else: rope slack -> no spring or directional rope damping

        // Air drag (isotropic viscous drag): F_air = -airDragC * v
        // This simulates small constant air resistance acting on the mass in any direction.
        if (airDragC > 0f)
        {
            Vector2 airDragForce = -airDragC * massBody.linearVelocity;
            totalForce += airDragForce;
        }

        // Apply force to the mass
        massBody.AddForce(totalForce, ForceMode2D.Force);

        // Optional line renderer for visualization
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, anchorPos);
            lineRenderer.SetPosition(1, massPos);
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // keep safe ranges in editor
        restLength = Mathf.Max(0f, restLength);
        minLength = Mathf.Max(0.00001f, minLength);
        springK = Mathf.Max(0f, springK);
        dampingC = Mathf.Max(0f, dampingC);
        airDragC = Mathf.Max(0f, airDragC);
    }
#endif
}