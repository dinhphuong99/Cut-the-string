using UnityEngine;

/// <summary>
/// PendulumModule — rope-aware, force-based pendulum module.
/// - Reads anchor, bob and ideal length from RopePendulumAdapter when available.
/// - Applies Hooke spring when current distance >= restLength (rope taut).
/// - Applies directional damping along rope and optional isotropic air drag.
/// - Does NOT mutate rope state or set transforms.
/// </summary>
[DisallowMultipleComponent]
public class PendulumModule : MonoBehaviour, IRopeModule
{
    [Header("Runtime Adapter (auto-wired if present on same GameObject)")]
    [Tooltip("Adapter that exposes anchor, bob and rope-derived config.")]
    public RopePendulumAdapter adapter;

    // Local serialized fallbacks (used if adapter is missing)
    [Header("Fallbacks (used only when adapter not present)")]
    public Transform anchor;
    public Rigidbody2D massBody;
    public float restLength = 2f;
    public float springK = 40f;

    [Header("Damping")]
    public float dampingC = 5f;
    public bool useAutoDamping = false;
    [Tooltip("When useAutoDamping=true, multiplier applied to critical damping (1 = critical).")]
    [Range(0f, 3f)]
    public float autoDampingFactor = 0.5f;

    [Header("Air drag (viscous)")]
    [Tooltip("Isotropic air drag coefficient (N·s/m).")]
    public float airDragC = 0.1f;

    [Header("Limits & Safety")]
    [Tooltip("Minimum distance to avoid divide-by-zero.")]
    public float minLength = 0.05f;

    [Tooltip("Maximum allowed stretch relative to restLength. <= 0 means no hard clamp.")]
    public float maxStretch = 0f;

    [Header("Visual (optional)")]
    public LineRenderer lineRenderer;

    // internal
    private bool initialized = false;
    private float bodyMass = 1f;

    // Convenience accessors that prefer adapter values
    private Transform Anchor => (adapter != null && adapter.Anchor != null) ? adapter.Anchor : anchor;
    private Rigidbody2D Bob => (adapter != null && adapter.Bob != null) ? adapter.Bob : massBody;
    private float IdealLengthFromAdapter => (adapter != null) ? adapter.IdealLength : restLength;
    private PendulumBehaviorProfile BehaviorFromAdapter => (adapter != null) ? adapter.pendulumBehavior : null;

    public void Initialize(IRopeDataProvider provider)
    {
        if (initialized) return;

        if (adapter == null)
        {
            // if provider is a VerletRope7 instance, try find adapter on same GO
            if (provider is VerletRope7 rp)
            {
                adapter = rp.GetComponent<RopePendulumAdapter>();
            }

            // fallback: try to find adapter on this GO
            if (adapter == null)
                adapter = GetComponent<RopePendulumAdapter>();
        }

        // if still null, we can still run using serialized fallbacks
        if (adapter == null)
        {
            // noop: run with manual anchor/massBody if assigned
            Debug.Log($"[PendulumModule] No RopePendulumAdapter found - falling back to serialized anchor/bob if present ({name}).", this);
        }

        // If Bob is available now, cache mass and compute auto-damping if requested
        Rigidbody2D rb = Bob;
        if (rb != null)
        {
            bodyMass = Mathf.Max(0.0001f, rb.mass);
        }

        // Don't override user-specified dampingC field; compute effective damping each step.
        initialized = true;
    }

    void Awake()
    {
        // keep Awake light: do not require adapter present at Awake
        // We will auto-wire adapter in Initialize (called from VerletRope7) or we can try here for safety.
        if (adapter == null)
            adapter = GetComponent<RopePendulumAdapter>();
    }

    void FixedUpdate()
    {
        // Resolve runtime anchor and bob (adapter preferred)
        Transform runtimeAnchor = Anchor;
        Rigidbody2D runtimeBob = Bob;

        if (runtimeAnchor == null || runtimeBob == null)
            return;

        // Ensure we have current mass
        bodyMass = Mathf.Max(0.0001f, runtimeBob.mass);

        // Determine effective parameters (adapter.behavior can tweak these)
        PendulumBehaviorProfile beh = BehaviorFromAdapter;
        float effectiveSpringK = springK;
        float effectiveDampingBase = dampingC;
        float effectiveAirDrag = airDragC;

        if (beh != null)
        {
            // behavior profile provides multipliers / overrides
            effectiveSpringK *= Mathf.Max(0.00001f, beh.springKMultiplier);
            effectiveDampingBase *= Mathf.Max(0f, beh.dampingAlongMultiplier);
            // behavior may provide explicit air drag override if > 0
            if (beh.airDragC > 0f) effectiveAirDrag = beh.airDragC;
        }

        // Determine rest length: prefer adapter ideal length when available
        float rest = IdealLengthFromAdapter;
        if (rest <= Mathf.Epsilon)
            rest = Mathf.Max(0.0001f, restLength);

        // Compute current geometry
        Vector2 anchorPos = (Vector2)runtimeAnchor.position;
        Vector2 bobPos = runtimeBob.position;
        Vector2 delta = bobPos - anchorPos;
        float currentLen = delta.magnitude;

        if (currentLen < minLength)
            return;

        Vector2 dir = delta / currentLen;

        // Activation condition: only apply spring/directional damping when rope is taut
        bool isTaut = currentLen >= rest - 1e-6f;

        // Compute extension (only positive)
        float extension = Mathf.Max(0f, currentLen - rest);
        if (maxStretch > 0f)
            extension = Mathf.Clamp(extension, 0f, maxStretch);

        Vector2 totalForce = Vector2.zero;

        if (isTaut && extension > Mathf.Epsilon)
        {
            // Spring (Hooke)
            float springForceScalar = -effectiveSpringK * extension;
            Vector2 springForce = dir * springForceScalar;

            // Directional damping along rope: use projected bob velocity
            float velAlong = Vector2.Dot(runtimeBob.linearVelocity, dir);
            float dampingC_eff;
            if (useAutoDamping)
            {
                float cCritical = 2f * Mathf.Sqrt(effectiveSpringK * bodyMass);
                dampingC_eff = cCritical * Mathf.Max(0f, autoDampingFactor);
            }
            else
            {
                dampingC_eff = effectiveDampingBase;
            }

            Vector2 dampingForce = -dampingC_eff * velAlong * dir;

            totalForce += springForce + dampingForce;
        }

        // Isotropic air drag
        if (effectiveAirDrag > 0f)
        {
            totalForce += -effectiveAirDrag * runtimeBob.linearVelocity;
        }

        // Apply to rigidbody (force-based)
        runtimeBob.AddForce(totalForce, ForceMode2D.Force);

        // Optional visualization
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, anchorPos);
            lineRenderer.SetPosition(1, bobPos);
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        restLength = Mathf.Max(0f, restLength);
        minLength = Mathf.Max(0.00001f, minLength);
        springK = Mathf.Max(0f, springK);
        dampingC = Mathf.Max(0f, dampingC);
        airDragC = Mathf.Max(0f, airDragC);
    }
#endif
}
