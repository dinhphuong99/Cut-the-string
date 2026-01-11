using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Dây mô phỏng bằng Verlet, có thể cắt ở bất kỳ node nào.
/// Không dùng Rigidbody thật, chỉ mô phỏng vị trí.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class VerletRope : MonoBehaviour
{
    [Header("Rope Parameters")]
    public int nodeCount = 20;
    public float segmentLength = 0.2f;
    public float gravity = 9.81f;
    public float damping = 0.995f;
    public int constraintIterations = 6;
    public float recoilStrength = 1.5f;
    public bool simulate = true;

    [Header("Visual")]
    public Transform startPoint;
    public Transform endPoint;
    public float slack = 1.1f; // 10% dài hơn để có độ võng
    private LineRenderer line;

    private List<Vector2> positions = new();
    private List<Vector2> oldPositions = new();

    void Start()
    {
        line = GetComponent<LineRenderer>();
        InitializeRope(startPoint.position, endPoint.position, slack);
    }

    public void InitializeRope(Vector2 start, Vector2 end, float slackFactor = 1f)
    {
        positions.Clear();
        oldPositions.Clear();

        float ropeLength = Vector2.Distance(start, end) * slackFactor;
        segmentLength = ropeLength / (nodeCount - 1);

        Vector2 dir = (end - start).normalized;
        for (int i = 0; i < nodeCount; i++)
        {
            Vector2 pos = start + dir * segmentLength * i;
            positions.Add(pos);
            oldPositions.Add(pos);
        }

        if (line)
            line.positionCount = nodeCount;
    }

    void FixedUpdate()
    {
        if (!simulate) return;
        SimulateVerlet(Time.fixedDeltaTime);
        ApplyConstraints();
        UpdateLineRenderer();
    }

    void SimulateVerlet(float dt)
    {
        Vector2 gravityVec = new Vector2(0, -gravity);

        for (int i = 1; i < nodeCount; i++) // node đầu cố định
        {
            Vector2 pos = positions[i];
            Vector2 old = oldPositions[i];

            Vector2 velocity = (pos - old) * damping;
            oldPositions[i] = pos;
            positions[i] = pos + velocity + gravityVec * dt * dt;
        }

        positions[0] = startPoint.position; // cố định đầu
        positions[nodeCount - 1] = endPoint.position; // đầu kia (nếu muốn cố định)
    }

    void ApplyConstraints()
    {
        for (int iter = 0; iter < constraintIterations; iter++)
        {
            for (int i = 0; i < nodeCount - 1; i++)
            {
                Vector2 p1 = positions[i];
                Vector2 p2 = positions[i + 1];
                Vector2 delta = p2 - p1;
                float dist = delta.magnitude;
                float diff = (dist - segmentLength) / dist;

                if (i > 0)
                    positions[i] += delta * diff * 0.5f;
                else
                    positions[i] = startPoint.position;

                if (i + 1 < nodeCount - 1)
                    positions[i + 1] -= delta * diff * 0.5f;
                else
                    positions[i + 1] = endPoint.position;
            }
        }
    }

    void UpdateLineRenderer()
    {
        if (!line) return;
        for (int i = 0; i < nodeCount; i++)
            line.SetPosition(i, positions[i]);
    }

    // ---------------- CUTTING SYSTEM ---------------- //

    public void CutAtNode(int index)
    {
        if (index <= 0 || index >= nodeCount - 1) return;

        // 1. Tách danh sách
        List<Vector2> left = new(positions.GetRange(0, index + 1));
        List<Vector2> right = new(positions.GetRange(index, nodeCount - index));

        // 2. Nhân đôi node cắt
        Vector2 cutPos = positions[index];
        left[left.Count - 1] = cutPos;
        right[0] = cutPos;

        // 3. Tạo recoil
        Vector2 dir = (positions[Mathf.Min(index + 1, nodeCount - 1)] - positions[Mathf.Max(index - 1, 0)]).normalized;
        ApplyRecoil(left, -dir * recoilStrength);
        ApplyRecoil(right, dir * recoilStrength);

        // 4. Tạo hai dây mới
        SpawnRopePiece(left, "LeftPiece");
        SpawnRopePiece(right, "RightPiece");

        // 5. Xoá dây gốc
        Destroy(gameObject);
    }

    void ApplyRecoil(List<Vector2> nodes, Vector2 recoil)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            float falloff = (float)i / nodes.Count;
            nodes[i] += recoil * (1f - falloff) * 0.1f; // tạo chuyển động nhẹ dần
        }
    }

    void SpawnRopePiece(List<Vector2> nodePositions, string nameSuffix)
    {
        GameObject go = new GameObject("RopePiece_" + nameSuffix);
        var rope = go.AddComponent<VerletRope>();
        var lr = go.AddComponent<LineRenderer>();

        lr.material = line.material;
        lr.widthMultiplier = line.widthMultiplier;

        rope.line = lr;
        rope.nodeCount = nodePositions.Count;
        rope.segmentLength = segmentLength;
        rope.gravity = gravity;
        rope.damping = damping;
        rope.constraintIterations = constraintIterations;
        rope.simulate = true;

        // Copy node positions vào rope mới
        rope.positions = new List<Vector2>(nodePositions);
        rope.oldPositions = new List<Vector2>(nodePositions);

        // Ở đây hai đầu tự do
        rope.startPoint = null;
        rope.endPoint = null;
    }

    // Debug key: cắt giữa dây
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
            CutAtNode(nodeCount / 2);
    }
}