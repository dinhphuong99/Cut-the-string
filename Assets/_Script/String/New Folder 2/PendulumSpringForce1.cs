using UnityEngine;

public class PendulumSpringForce1 : MonoBehaviour
{
    [Header("References")]
    public Transform mount;
    public RopePhysicsProfile profile;
    public bool drawLine = true;

    [SerializeField]private Rigidbody2D bob;
    private RopeForceSolver solver;
    private Vector2 vel;
    private Vector2 pos;

    private float restLength = 4f;

    void Start()
    {
        //bob = GetComponent<Rigidbody2D>();
        if (mount == null)
        {
            Debug.LogError("Missing mount Transform!");
            enabled = false;
            return;
        }

        if (profile == null)
        {
            Debug.LogWarning("No RopePhysicsProfile assigned — using default one.");
            profile = ScriptableObject.CreateInstance<RopePhysicsProfile>();
        }

        solver = new RopeForceSolver(profile);

        // Tính restLength động theo khoảng cách giữa mount và bob
        restLength = Vector2.Distance(mount.position, bob.position);

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
            solver.IntegrateForce(ref pos, ref vel, bob, pivot, restLength, dampingPerSub, dt);
            solver.ClampStretch(ref pos, ref vel, pivot, restLength);
        }

        // Cập nhật Rigidbody2D
        bob.MovePosition(pos);
        bob.linearVelocity = (pos - oldPos) / fixedDt;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (drawLine && mount && bob)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(mount.position, bob.position);
        }
    }
#endif
}