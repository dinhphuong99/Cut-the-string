using UnityEngine;

public class PendulumSpringForce2 : MonoBehaviour
{
    [Header("References")]
    public Transform mount;
    public Rigidbody2D bob;
    public RopePhysicsProfile profile;
    public bool drawLine = true;

    [HideInInspector] public float dist;

    private RopeForceSolver solver;
    private Vector2 vel;
    private Vector2 pos;
    private Vector2 pivot;
    private float restLength = 4f;

    void Start()
    {
        if (bob == null)
        {
            Debug.LogError("Missing Rigidbody2D bob reference!");
            enabled = false;
            return;
        }

        if (profile == null)
        {
            Debug.LogWarning("No RopePhysicsProfile assigned — using default instance.");
            profile = ScriptableObject.CreateInstance<RopePhysicsProfile>();
        }

        solver = new RopeForceSolver(profile);
        bob.gravityScale = 0f;
    }

    void FixedUpdate()
    {
        SimulatePendulum();
    }

    void SimulatePendulum()
    {
        float fixedDt = Time.fixedDeltaTime;
        int substeps = Mathf.Max(1, profile.substeps);
        float dt = fixedDt / substeps;
        float dampingPerSub = Mathf.Pow(profile.damping, 1f / substeps);

        Vector2 oldPos = bob.position;
        pos = oldPos;
        vel = bob.linearVelocity;
        pivot = mount.position;

        for (int i = 0; i < substeps; i++)
        {
            solver.IntegrateForce(ref pos, ref vel, bob, pivot, dampingPerSub, dt, restLength);
            solver.ClampStretch(ref pos, ref vel, pivot, restLength);
        }

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