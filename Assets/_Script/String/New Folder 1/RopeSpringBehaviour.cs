using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RopeSpringBehaviour : MonoBehaviour
{
    [Header("Anchors")]
    public Transform startPoint;               // Đầu cố định
    public Rigidbody2D endBody;                // Đầu động có Rigidbody2D

    [Header("Rope Settings")]
    public int nodeCount = 20;                 // Số điểm chia dây
    public float restLength = 0.2f;            // Chiều dài mỗi segment (L0)
    public float springStrength = 200f;        // Hằng số đàn hồi (k)
    public float damping = 0.9f;               // Giảm chấn tổng thể
    public float nodeMass = 0.05f;             // Khối lượng mỗi node
    public Vector2 gravity = new Vector2(0, -9.81f);

    [Header("Solver Settings")]
    public int constraintIterations = 10;      // Số vòng chỉnh dây
    public bool visualizeNodes = false;        // Debug node bằng Gizmo

    private Node[] nodes;
    private LineRenderer line;

    private struct Node
    {
        public Vector2 position;
        public Vector2 previousPosition;
        public Vector2 velocity;

        public Node(Vector2 pos)
        {
            position = pos;
            previousPosition = pos;
            velocity = Vector2.zero;
        }
    }

    void Start()
    {
        line = GetComponent<LineRenderer>();
        InitializeRope();
    }

    void InitializeRope()
    {
        nodes = new Node[nodeCount];
        Vector2 dir = (endBody.position - (Vector2)startPoint.position).normalized;
        float totalLength = Vector2.Distance(startPoint.position, endBody.position);
        restLength = totalLength / (nodeCount - 1);

        for (int i = 0; i < nodeCount; i++)
        {
            Vector2 pos = (Vector2)startPoint.position + dir * restLength * i;
            nodes[i] = new Node(pos);
        }

        line.positionCount = nodeCount;
    }

    void FixedUpdate()
    {
        Simulate(Time.fixedDeltaTime);
        DrawRope();
    }

    void Simulate(float dt)
    {
        // --- 1. Tích phân lực lên node ---
        for (int i = 1; i < nodeCount - 1; i++) // bỏ đầu và cuối
        {
            Vector2 velocity = nodes[i].velocity;
            velocity += gravity * dt;
            velocity *= damping;

            nodes[i].position += velocity * dt;
            nodes[i].velocity = velocity;
        }

        // --- 2. Cập nhật node cuối theo vị trí Rigidbody2D ---
        nodes[nodeCount - 1].position = endBody.position;

        // --- 3. Giải ràng buộc đàn hồi ---
        for (int iter = 0; iter < constraintIterations; iter++)
        {
            ApplySpringForces(dt);
        }

        // --- 4. Truyền phản lực thật lên Rigidbody2D ---
        Vector2 ropeForce = ComputeEndForce();
        endBody.AddForce(ropeForce);
    }

    void ApplySpringForces(float dt)
    {
        for (int i = 0; i < nodeCount - 1; i++)
        {
            Vector2 p1 = nodes[i].position;
            Vector2 p2 = nodes[i + 1].position;

            Vector2 delta = p2 - p1;
            float dist = delta.magnitude;
            float stretch = dist - restLength;

            Vector2 forceDir = delta.normalized;
            Vector2 springForce = forceDir * (springStrength * stretch);

            // Cập nhật vị trí node dựa trên lực
            if (i != 0) // node đầu cố định
                nodes[i].position += springForce / nodeMass * dt * dt * 0.5f;
            if (i + 1 != nodeCount - 1) // node cuối bị Rigidbody chi phối
                nodes[i + 1].position -= springForce / nodeMass * dt * dt * 0.5f;
        }

        // Đặt lại đầu đầu & cuối
        nodes[0].position = startPoint.position;
        nodes[nodeCount - 1].position = endBody.position;
    }

    Vector2 ComputeEndForce()
    {
        Node n1 = nodes[nodeCount - 2];
        Node n2 = nodes[nodeCount - 1];

        Vector2 delta = n2.position - n1.position;
        float dist = delta.magnitude;
        float stretch = dist - restLength;
        Vector2 dir = delta.normalized;

        Vector2 force = -dir * (springStrength * stretch);
        return force;
    }

    void DrawRope()
    {
        for (int i = 0; i < nodeCount; i++)
        {
            line.SetPosition(i, nodes[i].position);
        }
    }

    void OnDrawGizmos()
    {
        if (!visualizeNodes || nodes == null) return;
        Gizmos.color = Color.yellow;
        foreach (var n in nodes)
        {
            Gizmos.DrawSphere(n.position, 0.02f);
        }
    }
}