using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RopeHybridBehaviour : MonoBehaviour
{
    public Transform startPoint;
    public Rigidbody2D endRigidbody; // <-- vật nặng thật
    public int nodeCount = 20;
    public float segmentLength = 0.2f;
    public float segmentLengthMax = 0.25f;
    public float damping = 0.98f;
    public Vector3 gravity = new Vector3(0, -9.81f, 0);
    public int constraintIterations = 20;

    [Header("Hybrid Bridge")]
    [Tooltip("How strongly the rope tries to enforce the end position on the rigidbody (0..1).")]
    [Range(0f, 1f)]
    public float bridgeStiffness = 0.8f; // 0 = very soft, 1 = very stiff (careful)
    public float maxImpulse = 50f; // clamp impulse to avoid spikes

    private Node[] nodes;
    private LineRenderer line;

    private struct Node
    {
        public Vector3 position;
        public Vector3 previousPosition;
        public float mass;

        public Node(Vector3 pos, float mass)
        {
            position = pos;
            previousPosition = pos;
            this.mass = mass;
        }
    }

    void Start()
    {
        line = GetComponent<LineRenderer>();
        InitializeNodes();
    }

    void InitializeNodes()
    {
        float totalLength = Vector3.Distance(startPoint.position, endRigidbody.position);
        nodeCount = Mathf.Max(2, (int)(totalLength / segmentLength) + 1);
        int totalNodes = Mathf.CeilToInt(nodeCount * 1.05f);
        nodes = new Node[totalNodes];

        Vector3 dir = ((Vector3)endRigidbody.position - startPoint.position).normalized;
        float spacing = totalLength / (totalNodes - 1);

        for (int i = 0; i < totalNodes; i++)
        {
            Vector3 pos = startPoint.position + dir * spacing * i;
            nodes[i] = new Node(pos, 1f);
        }

        line.positionCount = totalNodes;
    }

    // FixedUpdate: make physics deterministic and in sync with Rigidbody
    void FixedUpdate()
    {
        Simulate(Time.fixedDeltaTime);
    }

    // Draw in Update so render is smooth
    void Update()
    {
        DrawRope();
    }

    void Simulate(float dt)
    {
        // Verlet integration for inner nodes (skip anchors)
        for (int i = 1; i < nodes.Length - 1; i++)
        {
            Vector3 velocity = (nodes[i].position - nodes[i].previousPosition) * damping;

            float distanceWithHeadNode = 0f;
            if(i > 0)
            {
                distanceWithHeadNode = Vector3.Distance(nodes[i].position, nodes[i - 1].position);
            }

            if (distanceWithHeadNode <= segmentLengthMax)
            {
                nodes[i].previousPosition = nodes[i].position;
                nodes[i].position += velocity + gravity * dt * dt;
            }

        }

        // Anchor start to static startPoint
        nodes[0].position = startPoint.position;

        // Note: DO NOT snap last node to rigidbody.position directly.
        // We'll use a soft constraint -> apply impulse to Rigidbody instead.

        // Apply constraints iteratively (including soft bridge with rigidbody)
        for (int iter = 0; iter < constraintIterations; iter++)
        {
            ApplyConstraints(dt);
        }
            
    }

    void ApplyConstraints(float dt)
    {
        int lastIndex = nodes.Length - 1;

        // Standard segment constraints (internal)
        for (int i = 0; i < nodes.Length - 1; i++)
        {
            Vector3 delta = nodes[i + 1].position - nodes[i].position;
            float dist = delta.magnitude;
            if (dist == 0f) continue;

            float diff = (dist - segmentLength) / dist;
            Vector3 correction = delta * diff;

            bool firstFixed = (i == 0);
            bool lastFixed = (i + 1 == lastIndex);

            // For internal segments, split correction equally (could weight by mass)
            if (!firstFixed)
                nodes[i].position += correction * 0.5f;
            if (!lastFixed)
                nodes[i + 1].position -= correction * 0.5f;
        }

        // ----- Hybrid bridge constraint: connect nodes[lastIndex] <-> endRigidbody -----
        // Compute positional error between rope end and rigidbody
        Vector2 rbPos2 = endRigidbody.position;
        Vector3 nodePos = nodes[lastIndex].position;
        Vector3 deltaEnd = nodePos - (Vector3)rbPos2;
        float distEnd = deltaEnd.magnitude;

        // if too close or zero, nothing to do
        if (distEnd > 1e-6f)
        {
            // desired correction to make segment length valid between last-1 and last
            // But since endRigidbody is dynamic, we'll *convert part of positional correction to an impulse*.
            // Compute target correction that would be applied to node and rigidbody in equal share if both free.
            Vector3 correction = deltaEnd * (1f - (segmentLength / distEnd));

            // Distribute correction: we move rope node a bit and push rigidbody by the rest via impulse.
            // Move rope node (softly) to reduce immediate visual error:
            float nodeMoveFactor = 0.5f * (1f - bridgeStiffness); // if bridgeStiffness high, node moves less
            nodes[lastIndex].position -= correction * nodeMoveFactor;

            // Compute velocity change needed on rigidbody to close the remaining gap:
            Vector3 remaining = correction * (1f - nodeMoveFactor); // amount we want rb to "absorb"
            Vector2 deltaV = (Vector2)(remaining / Mathf.Max(dt, 1e-6f)); // v = dx / dt

            // Impulse = mass * deltaV
            float rbMass = Mathf.Max(0.0001f, endRigidbody.mass);
            Vector2 impulse = deltaV * rbMass;

            // Apply stiffness scaling and clamp
            impulse *= bridgeStiffness;
            if (impulse.magnitude > maxImpulse) impulse = impulse.normalized * maxImpulse;

            // Apply as impulse so it updates velocity immediately
            endRigidbody.AddForce(impulse, ForceMode2D.Impulse);
        }

        // Re-anchor start node strictly
        nodes[0].position = startPoint.position;
    }

    void DrawRope()
    {
        for (int i = 0; i < nodes.Length; i++)
            line.SetPosition(i, nodes[i].position);
    }
}