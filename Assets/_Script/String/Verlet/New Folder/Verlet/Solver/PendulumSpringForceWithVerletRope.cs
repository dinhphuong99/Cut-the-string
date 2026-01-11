using UnityEngine;

/// <summary>
/// Mô phỏng con lắc hoặc dây/lò xo đơn giản giữa mount và bob.
/// Sử dụng RopeForceSolver để tính lực Hooke + Gravity.
/// </summary>

[RequireComponent(typeof(VerletRope7))]
public class PendulumSpringForceWithVerletRope : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Điểm gắn cố định đầu trên của dây.")]
    public Transform mount;

    [Tooltip("Hồ sơ vật lý chứa các tham số lực, damping, substeps,...")]
    public RopePhysicsProfile profile;

    [SerializeField] private Rigidbody2D bob;
    private RopeForceSolver1 solver;

    private Vector2 pos;
    private Vector2 vel;
    [SerializeField] private float restLength;

    void Start()
    {

        if (mount == null)
        {
            Debug.LogError("[PendulumSpringForce2] Missing mount Transform!");
            enabled = false;
            return;
        }

        if (profile == null)
        {
            Debug.LogWarning("[PendulumSpringForce2] No RopePhysicsProfile assigned — using default.");
            profile = ScriptableObject.CreateInstance<RopePhysicsProfile>();
        }

        solver = new RopeForceSolver1(profile);

        // Tính restLength động từ vị trí ban đầu
        restLength = Vector2.Distance(mount.position, bob.position);

        // Tắt gravity nội bộ để tránh chồng lực
        bob.gravityScale = 0f;

        pos = bob.position;
        vel = bob.linearVelocity;
    }

    void FixedUpdate()
    {
        Simulate();
    }

    private void Simulate()
    {
        float fixedDt = Time.fixedDeltaTime;
        int substeps = Mathf.Max(1, profile.substeps);
        float dt = fixedDt / substeps;
        float dampingPerSub = Mathf.Pow(profile.damping, 1f / substeps);

        Vector2 pivot = mount.position;
        Vector2 oldPos = pos;

        for (int i = 0; i < substeps; i++)
        {
            solver.IntegrateForce(ref pos, ref vel, bob.mass, pivot, restLength, dampingPerSub, dt);
            //solver.ClampStretch(ref pos, ref vel, pivot, restLength);
        }

        // Cập nhật lại Rigidbody2D
        bob.MovePosition(pos);
        bob.linearVelocity = (pos - oldPos) / fixedDt;
    }

    public void ApplyLengthChangeSafely(float newRestLength, float pivotDragSpeed)
    {
        // 1. Tính dt cho nội suy
        float dt = Time.fixedDeltaTime;

        // 2. Interpolate chiều dài để tránh spike lực
        float oldRest = restLength;
        restLength = Mathf.MoveTowards(oldRest, newRestLength, pivotDragSpeed * dt);

        // 3. Bắt buộc projection vào vòng tròn bán kính restLength mới
        Vector2 pivot = mount.position;
        Vector2 dir = pos - pivot;
        float dist = dir.magnitude;

        if (dist > 1e-6f)
        {
            dir /= dist;

            // Projection: bob nằm đúng trên dây mới
            Vector2 newPos = pivot + dir * restLength;

            // 4. Bảo toàn vận tốc tiếp tuyến
            Vector2 radialVel = Vector2.Dot(vel, dir) * dir;
            Vector2 tangentialVel = vel - radialVel;

            // Loại radial velocity → tránh nổ vật lý
            vel = tangentialVel;

            // Update pos
            pos = newPos;
        }
        else
        {
            // Bob trùng pivot → reset an toàn
            pos = pivot + new Vector2(restLength, 0);
            vel = Vector2.zero;
        }
    }

}