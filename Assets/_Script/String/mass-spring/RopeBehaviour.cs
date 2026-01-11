using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Quản lý sợi dây vật lý, cập nhật các node, vẽ bằng LineRenderer.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class RopeBehaviour : MonoBehaviour
{
    [Header("References")]
    public Transform startPoint;
    public Transform endPoint;
    public RopeNodeSettings settings;

    [Header("Rope Parameters")]
    public int nodeCount = 20;
    private List<RopeNode> nodes = new List<RopeNode>();

    [Header("Rendering")]
    public LineRenderer lineRenderer;
    public Gradient ropeColor;
    public float ropeWidth = 0.05f;

    private void Awake()
    {
        // Tự động gán LineRenderer nếu chưa có
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer != null)
        {
            lineRenderer.startWidth = ropeWidth;
            lineRenderer.endWidth = ropeWidth;
            lineRenderer.colorGradient = ropeColor;
        }
    }

    private void Start()
    {
        InitializeRope();
    }

    private void FixedUpdate()
    {
        SimulateRope(Time.fixedDeltaTime);
        DrawRope();
    }

    /// <summary>
    /// Tạo danh sách node của dây và liên kết các node liền kề.
    /// </summary>
    private void InitializeRope()
    {
        nodes.Clear();

        Vector3 direction = (endPoint.position - startPoint.position).normalized;
        float totalLength = Vector3.Distance(startPoint.position, endPoint.position);
        float segmentLength = totalLength / (nodeCount - 1);

        for (int i = 0; i < nodeCount; i++)
        {
            Vector3 pos = startPoint.position + direction * segmentLength * i;
            bool fixedNode = (i == 0 || i == nodeCount - 1);
            float priority = (i == 0) ? 1.0f : (i == nodeCount - 1 ? 0.8f : 0f);

            nodes.Add(new RopeNode(pos, settings, fixedNode, priority));
        }

        // Gán liên kết hàng xóm
        for (int i = 0; i < nodes.Count; i++)
        {
            if (i > 0) nodes[i].head = nodes[i - 1];
            if (i < nodes.Count - 1) nodes[i].tail = nodes[i + 1];
            if (i > 1) nodes[i].headAdjacent = nodes[i - 2];
            if (i < nodes.Count - 2) nodes[i].tailAdjacent = nodes[i + 2];
        }
    }

    /// <summary>
    /// Mô phỏng vật lý cho dây, dựa trên các lực đã định nghĩa.
    /// </summary>
    private void SimulateRope(float dt)
    {
        foreach (var node in nodes)
        {
            if (node.isFixed) continue;

            node.ComputeForces();

            //// Ưu tiên lực cho đầu dây có priority cao
            //if (node.head != null && node.head.isFixed)
            //    force *= Mathf.Lerp(1f, node.priority, 0.5f);

            //// Euler integration
            //Vector3 acceleration = force / settings.mass;
            //node.velocity += acceleration * dt;
            //node.position += node.velocity * dt;

            //// --- Debug kiểm tra lỗi vật lý ---
            //if (float.IsNaN(node.position.x) || float.IsNaN(node.position.y) || float.IsNaN(node.position.z))
            //{
            //    Debug.LogError($"[Rope Debug] Node {nodes.IndexOf(node)} has NaN position! Resetting to startPoint.");
            //    node.position = startPoint.position; // hoặc gán lại vị trí an toàn
            //    node.velocity = Vector3.zero;
            //}
            //else if (node.position.sqrMagnitude > 1e8f) // khoảng 10,000 đơn vị từ gốc
            //{
            //    Debug.LogWarning($"[Rope Debug] Node {nodes.IndexOf(node)} drifted too far! ({node.position})");
            //    node.position = Vector3.ClampMagnitude(node.position, 1000f);
            //    node.velocity *= 0.5f; // giảm động năng tránh nổ tiếp
            //}
        }

        // Gắn hai đầu vào object
        nodes[0].position = startPoint.position;
        nodes[^1].position = endPoint.position;
    }

    /// <summary>
    /// Vẽ dây bằng LineRenderer theo vị trí các node hiện tại.
    /// </summary>
    private void DrawRope()
    {
        if (lineRenderer == null || nodes == null || nodes.Count == 0) return;

        lineRenderer.positionCount = nodes.Count;
        for (int i = 0; i < nodes.Count; i++)
        {
            lineRenderer.SetPosition(i, nodes[i].position);
        }
    }

#if UNITY_EDITOR
    // Dành cho debug trong Scene View
    private void OnDrawGizmos()
    {
        if (nodes == null || nodes.Count == 0) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < nodes.Count - 1; i++)
        {
            Gizmos.DrawLine(nodes[i].position, nodes[i + 1].position);
        }
    }
#endif
}