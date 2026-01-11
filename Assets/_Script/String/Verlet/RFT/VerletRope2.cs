using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Dây mô phỏng bằng Verlet integration.
/// Không dùng Rigidbody thật, chỉ mô phỏng vị trí.
/// Có thể cắt, spawn lại các đoạn.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class VerletRope2 : MonoBehaviour
{
    [Header("Physics Profile (Optional)")]
    public RopePhysicsProfile physicsProfile;

    [Header("Rope Parameters")]
    public int nodeCount = 20;
    [HideInInspector] public float segmentLength = 0.2f;
    [HideInInspector] public float gravity = 9.81f;
    [HideInInspector] [Range(0.8f, 0.9999f)] public float damping = 0.995f;
    [HideInInspector] public int constraintIterations = 6;
    [HideInInspector] public float slack = 1.1f;
    [HideInInspector] public float recoilStrength = 1.5f;

    [Header("Simulation")]
    public bool simulate = true;

    [Header("Visual")]
    public Transform startPoint;
    public Transform endPoint;

    private LineRenderer line;
    private List<Vector2> positions = new();
    private List<Vector2> oldPositions = new();

    void Start()
    {
        line = GetComponent<LineRenderer>();

        if (physicsProfile != null)
            ApplyProfile();

        if (startPoint && endPoint)
            InitializeRope(startPoint.position, endPoint.position, slack);
        else
            Debug.LogWarning($"[{name}] Rope missing startPoint or endPoint reference!");
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Tự đồng bộ khi chỉnh Inspector
        if (physicsProfile != null)
            ApplyProfile();
    }
#endif

    void ApplyProfile()
    {
        gravity = physicsProfile.gravity;
        damping = physicsProfile.damping;
        segmentLength = physicsProfile.segmentLength;
        constraintIterations = physicsProfile.constraintIterations;
        slack = physicsProfile.slackFactor;
    }

    public void InitializeRope(Vector2 start, Vector2 end, float slackFactor = 1f)
    {
        positions.Clear();
        oldPositions.Clear();

        float ropeLength = Vector2.Distance(start, end) * slackFactor;
        nodeCount = Mathf.Max(2, (int)(ropeLength / segmentLength  +1));

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
        if (!simulate || positions.Count == 0) return;

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

        if (startPoint) positions[0] = startPoint.position;
        if (endPoint) positions[nodeCount - 1] = endPoint.position;
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
                else if (startPoint)
                    positions[i] = startPoint.position;

                if (i + 1 < nodeCount - 1)
                    positions[i + 1] -= delta * diff * 0.5f;
                else if (endPoint)
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
        if (index <= 0 || index >= nodeCount - 1)
            return;

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
            nodes[i] += recoil * (1f - falloff) * 0.1f;
        }
    }

    void SpawnRopePiece(List<Vector2> nodePositions, string nameSuffix)
    {
        GameObject go = new GameObject("RopePiece_" + nameSuffix);
        var rope = go.AddComponent<VerletRope2>();
        var lr = go.AddComponent<LineRenderer>();
        rope.physicsProfile = physicsProfile;
        if (physicsProfile != null)
            rope.ApplyProfile();

        // Nếu rope gốc chưa có line, tạo line tạm để tránh null
        if (line == null)
            line = GetComponent<LineRenderer>() ?? gameObject.AddComponent<LineRenderer>();

        //if (lr == null)
            //lr = GetComponent<LineRenderer>() ?? gameObject.AddComponent<LineRenderer>();

        // Copy các thông số visual
        CopyLineRendererSettings(line, lr);

        rope.line = lr;
        rope.nodeCount = nodePositions.Count;
        lr.positionCount = rope.nodeCount;

        rope.simulate = true;

        rope.positions = new List<Vector2>(nodePositions);
        rope.oldPositions = new List<Vector2>(nodePositions);

        rope.startPoint = null;
        rope.endPoint = null;

    }



    // Debug key
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
            CutAtNode(nodeCount / 2);
    }

    void CopyLineRendererSettings(LineRenderer src, LineRenderer dst)
    {
        if (!src || !dst) return;
        if (src.material != null)
            dst.material = new Material(src.material);
        dst.widthMultiplier = src.widthMultiplier;
        dst.colorGradient = src.colorGradient != null ? src.colorGradient : new Gradient();
        dst.textureMode = src.textureMode;
        dst.numCornerVertices = src.numCornerVertices;
        dst.numCapVertices = src.numCapVertices;
        dst.alignment = src.alignment;
        dst.shadowCastingMode = src.shadowCastingMode;
        dst.receiveShadows = src.receiveShadows;
        dst.sortingLayerID = src.sortingLayerID;
        dst.sortingOrder = src.sortingOrder;
    }

}