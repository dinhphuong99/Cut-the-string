using UnityEngine;

public class PendulumSpringForceProfiled : MonoBehaviour
{
    public Transform anchor;
    public RopePhysicsProfile profile;

    [Header("Spring Settings")]
    public float restLength = 3f;
    public float mass = 1f; // Bạn có thể dùng rb.mass cũng được

    [SerializeField] private Rigidbody2D rb;

    private void Awake()
    {
        if (rb == null)
            return;
        rb.gravityScale = 0f;        // Gravity tự tính theo profile
        rb.linearDamping = 0f;                // Damping tự tính
        rb.angularDamping = 0f;
    }

    private void FixedUpdate()
    {
        if (profile == null || anchor == null || rb == null) return;

        float dt = Time.fixedDeltaTime;
        int steps = Mathf.Max(1, profile.substeps);
        float stepDt = dt / steps;

        for (int i = 0; i < steps; i++)
        {
            SimulateStep(stepDt);
        }

        rb.linearVelocity *= profile.damping;
    }

    private void SimulateStep(float dt
        )
    {
        Vector2 bobPos = rb.position;
        Vector2 anchorPos = anchor.position;

        Vector2 dir = bobPos - anchorPos;
        float dist = dir.magnitude;
        if (dist < 0.0001f) return;
        dir /= dist;

        // Độ dãn x
        float x = dist - restLength;

        // Giới hạn độ dãn để chống explosion
        float maxStretch = restLength * profile.maxStretchFactor;
        if (dist > maxStretch)
        {
            dist = maxStretch;
            x = dist - restLength;
        }

        // Lò xo: kéo hoặc có nén tùy profile
        bool canPush = profile.bidirectionalElasticity || x > 0f;
        Vector2 springForce = canPush ? (-profile.k * x * dir) : Vector2.zero;

        // Gravity từ profile
        Vector2 gravityForce = new Vector2(0, -profile.gravity) * mass;

        // Damping multiplicative
        //rb.linearVelocity *= profile.damping;

        // Tổng lực
        Vector2 totalForce = springForce + gravityForce;

        rb.AddForce(totalForce, ForceMode2D.Force);

        // Clamp vận tốc để ổn định
        float vMag = rb.linearVelocity.magnitude;
        if (vMag > profile.maxVelocity)
            rb.linearVelocity = rb.linearVelocity.normalized * profile.maxVelocity;
    }

    // Hàm để bạn query tension cuối cùng nếu cần (debug hoặc rope break)
    //public Vector2 GetCurrentTension()
    //{
    //    Vector2 bobPos = rb.position;
    //    Vector2 anchorPos = anchor.position;

    //    Vector2 dir = bobPos - anchorPos;
    //    float dist = dir.magnitude;
    //    if (dist < 1e-6f) return Vector2.zero;

    //    dir /= dist;
    //    float x = dist - restLength;
    //    if (x <= 0f) return Vector2.zero;

    //    return -profile.k * x * dir;
    //}

}
