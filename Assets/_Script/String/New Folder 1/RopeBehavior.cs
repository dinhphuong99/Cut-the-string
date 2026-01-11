using UnityEngine;

/// <summary>
/// StringBehavior: mô phỏng sợi dây vật lý gồm nhiều node.
/// Triết lý thiết kế:
/// - Không tách class Node riêng để tránh overhead GameObject.
/// - Dữ liệu node được lưu trong struct nội bộ để dễ truy cập.
/// - Giảm chi phí update và GC.
/// - Code gọn gàng, dễ debug, dễ mở rộng.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class RopeBehavior : MonoBehaviour
{
    [Header("Dây cơ bản")]
    public Transform startPoint;      // Điểm neo đầu dây
    public Transform endPoint;        // Điểm neo cuối dây
    [Range(2, 100)] public int nodeCount = 20;
    public float segmentLength = 0.2f;
    public float stiffness = 0.5f;    // Độ căng đàn hồi
    public float damping = 0.98f;     // Giảm dao động
    public Vector3 gravity = new Vector3(0, -9.81f, 0);
    public int solverIterations = 6;  // Độ chính xác vật lý

    private LineRenderer lineRenderer;
    private Node[] nodes;

    // Định nghĩa node nội bộ (struct, không phải GameObject)
    private struct Node
    {
        public Vector3 position;
        public Vector3 previousPosition;
        public float mass;
        public bool isFixed;

        public Node(Vector3 pos, float mass, bool isFixed)
        {
            this.position = pos;
            this.previousPosition = pos;
            this.mass = mass;
            this.isFixed = isFixed;
        }
    }

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        InitializeNodes();
    }

    void Update()
    {
        SimulatePhysics(Time.deltaTime);
        ApplyConstraints();
        UpdateLineRenderer();
    }

    /// <summary>
    /// Khởi tạo node cho dây, chia đều giữa hai điểm neo.
    /// </summary>
    void InitializeNodes()
    {
        nodes = new Node[nodeCount];
        Vector3 direction = (endPoint.position - startPoint.position).normalized;
        float totalLength = Vector3.Distance(startPoint.position, endPoint.position);
        float actualSegment = totalLength / (nodeCount - 1);

        for (int i = 0; i < nodeCount; i++)
        {
            Vector3 pos = startPoint.position + direction * actualSegment * i;
            bool fixedNode = (i == 0 || i == nodeCount - 1); // 2 đầu cố định
            nodes[i] = new Node(pos, 1f, fixedNode);
        }
    }

    /// <summary>
    /// Cập nhật vị trí node theo nguyên lý Verlet integration.
    /// </summary>
    void SimulatePhysics(float deltaTime)
    {
        for (int i = 0; i < nodeCount; i++)
        {
            if (nodes[i].isFixed) continue;

            Vector3 current = nodes[i].position;
            Vector3 velocity = (nodes[i].position - nodes[i].previousPosition) * damping;
            Vector3 newPos = nodes[i].position + velocity + gravity * (deltaTime * deltaTime);

            nodes[i].previousPosition = current;
            nodes[i].position = newPos;
        }
    }

    /// <summary>
    /// Áp ràng buộc độ dài dây và độ căng đàn hồi.
    /// </summary>
    void ApplyConstraints()
    {
        for (int k = 0; k < solverIterations; k++)
        {
            for (int i = 0; i < nodeCount - 1; i++)
            {
                Node nodeA = nodes[i];
                Node nodeB = nodes[i + 1];

                Vector3 delta = nodeB.position - nodeA.position;
                float currentDistance = delta.magnitude;
                float error = currentDistance - segmentLength;

                Vector3 correction = delta.normalized * (error * 0.5f * stiffness);

                if (!nodeA.isFixed) nodeA.position += correction;
                if (!nodeB.isFixed) nodeB.position -= correction;

                nodes[i] = nodeA;
                nodes[i + 1] = nodeB;
            }

            // Giữ 2 đầu cố định
            nodes[0].position = startPoint.position;
            nodes[nodeCount - 1].position = endPoint.position;
        }
    }

    /// <summary>
    /// Cập nhật LineRenderer để hiển thị dây.
    /// </summary>
    void UpdateLineRenderer()
    {
        lineRenderer.positionCount = nodeCount;
        for (int i = 0; i < nodeCount; i++)
            lineRenderer.SetPosition(i, nodes[i].position);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (nodes == null) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < nodeCount; i++)
        {
            if (nodes[i].isFixed)
                Gizmos.color = Color.red;
            else
                Gizmos.color = Color.green;
            Gizmos.DrawSphere(nodes[i].position, 0.03f);
        }
    }
#endif
}