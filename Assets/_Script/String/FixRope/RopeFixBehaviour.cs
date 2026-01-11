using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RopeFixBehaviour : MonoBehaviour
{
    public Transform startPoint;
    public Transform endPoint;

    public int nodeCount = 20;
    public float segmentLength = 0.2f;
    public float damping = 0.98f;
    public Vector3 gravity = new Vector3(0, -9.81f, 0);
    public int constraintIterations = 20;

    [Header("Debug Info (Read Only)")]
    [SerializeField] private float averageSegmentDistance;
    [SerializeField] private float maxSegmentDistance;
    [SerializeField] private float minSegmentDistance;

    private Node[] nodes;
    private LineRenderer line;

    public enum AnchorMode
    {
        Fixed,
        Dynamic,
        Priority
    }

    public AnchorMode startMode = AnchorMode.Fixed;
    public AnchorMode endMode = AnchorMode.Dynamic;

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
        float totalLength = Vector3.Distance(startPoint.position, endPoint.position);
        nodeCount = (int)(totalLength / segmentLength) + 1;

        int totalNodes = Mathf.CeilToInt(nodeCount * 1.05f);
        nodes = new Node[totalNodes];

        Vector3 dir = (endPoint.position - startPoint.position).normalized;
        float spacing = totalLength / (totalNodes - 1);

        for (int i = 0; i < totalNodes; i++)
        {
            Vector3 pos = startPoint.position + dir * spacing * i;
            nodes[i] = new Node(pos, 1f);
        }

        line.positionCount = totalNodes;
    }

    void Update()
    {
        Simulate(Time.deltaTime);
        DrawRope();
        UpdateDebugInfo(); // cập nhật giá trị debug
    }

    void Simulate(float dt)
    {
        for (int i = 1; i < nodes.Length - 1; i++)
        {
            Vector3 velocity = (nodes[i].position - nodes[i].previousPosition) * damping;
            nodes[i].previousPosition = nodes[i].position;
            nodes[i].position += velocity + gravity * dt * dt;
        }

        nodes[0].position = startPoint.position;
        nodes[nodes.Length - 1].position = endPoint.position;

        for (int iter = 0; iter < constraintIterations; iter++)
            ApplyConstraints();
    }

    void ApplyConstraints()
    {
        for (int i = 0; i < nodes.Length - 1; i++)
        {
            Vector3 delta = nodes[i + 1].position - nodes[i].position;
            float dist = delta.magnitude;
            if (dist <= segmentLength) continue;

            float diff = (dist - segmentLength) / dist;
            Vector3 correction = delta * diff;

            bool firstFixed = (i == 0);
            bool lastFixed = (i + 1 == nodes.Length - 1);

            if (!firstFixed)
                nodes[i].position += correction * 0.5f;
            if (!lastFixed)
                nodes[i + 1].position -= correction * 0.5f;
        }

        nodes[0].position = startPoint.position;
        nodes[nodes.Length - 1].position = endPoint.position;
        ApplyEndConstraint();
    }

    void DrawRope()
    {
        for (int i = 0; i < nodes.Length; i++)
            line.SetPosition(i, nodes[i].position);
    }

    void UpdateDebugInfo()
    {
        if (nodes == null || nodes.Length < 2) return;

        float total = 0f;
        maxSegmentDistance = float.MinValue;
        minSegmentDistance = float.MaxValue;

        for (int i = 0; i < nodes.Length - 1; i++)
        {
            float dist = Vector3.Distance(nodes[i].position, nodes[i + 1].position);
            total += dist;
            if (dist > maxSegmentDistance) maxSegmentDistance = dist;
            if (dist < minSegmentDistance) minSegmentDistance = dist;
        }

        averageSegmentDistance = total / (nodes.Length - 1);
    }

    void ApplyEndConstraint()
    {
        float maxDist = segmentLength * (nodeCount - 1);
        Vector3 offset = endPoint.position - startPoint.position;
        float distance = offset.magnitude;

        // Trọng lực riêng cho endPoint (nếu muốn giả lập vật nặng)
        Vector3 pseudoGravity = gravity * 0.5f * Time.deltaTime; // 0.5f là trọng lượng tương đối
        endPoint.position += pseudoGravity;

        // Constraint giữ khoảng cách
        if (distance > maxDist)
        {
            Vector3 corrected = startPoint.position + offset.normalized * maxDist;
            endPoint.position = corrected;
        }
    }
}