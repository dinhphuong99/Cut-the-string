using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Node dữ liệu cho từng điểm của dây.
/// Mỗi node tự lưu vị trí hiện tại, vị trí cũ, và cờ isPinned (cố định hay không).
/// </summary>
[System.Serializable]
public struct RopeNode1
{
    public Vector2 position;
    public Vector2 oldPosition;
    public bool isPinned;

    public RopeNode1(Vector2 pos, bool pinned = false)
    {
        position = pos;
        oldPosition = pos;
        isPinned = pinned;
    }

    public Vector2 Velocity => position - oldPosition;
}

/// <summary>
/// Dây mô phỏng bằng Verlet integration.
/// Không dùng Rigidbody thật, chỉ mô phỏng vị trí.
/// Có thể cắt, spawn lại các đoạn.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class VerletRope4 : MonoBehaviour
{
    [Header("Physics Profile (Optional)")]
    public RopePhysicsProfile physicsProfile;

    [Header("Rope Parameters")]
    public int nodeCount = 20;
    [HideInInspector] public float segmentLength = 0.2f;
    [HideInInspector] public float gravity = 9.81f;
    [HideInInspector, Range(0.8f, 0.9999f)] public float damping = 0.995f;
    [HideInInspector] public int constraintIterations = 6;
    [HideInInspector] public float slack = 1.1f;
    [HideInInspector] public float recoilStrength = 1.5f;

    [Header("Simulation")]
    public bool simulate = true;

    [Header("Visual")]
    public Transform startPoint;
    public Transform endPoint;

    private LineRenderer line;
    private List<RopeNode1> nodes = new();

    void Start()
    {
        line = GetComponent<LineRenderer>();

        if (physicsProfile != null)
            ApplyProfile();

        // Nếu rope đã có node (spawn runtime) => không cần InitializeRope
        if (nodes != null && nodes.Count > 0)
        {
            nodeCount = nodes.Count;
            line.positionCount = nodeCount;
            UpdateLineRenderer();  // vẽ lại dây theo node sẵn có
            simulate = true;
        }
        else if (startPoint && endPoint)
        {
            InitializeRope(startPoint.position, endPoint.position, slack);
        }
        else
        {
            Debug.LogWarning($"[{name}] Rope missing startPoint or endPoint reference!");
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
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

    // -------------------------------------------------------------------------
    // Initialization
    // -------------------------------------------------------------------------
    public void InitializeRope(Vector2 start, Vector2 end, float slackFactor = 1f)
    {
        nodes.Clear();

        float ropeLength = Vector2.Distance(start, end) * slackFactor;
        nodeCount = Mathf.Max(2, (int)(ropeLength / segmentLength + 1));

        Vector2 dir = (end - start).normalized;
        for (int i = 0; i < nodeCount; i++)
        {
            Vector2 pos = start + dir * segmentLength * i;
            nodes.Add(new RopeNode1(pos));
        }

        // Pin node đầu/cuối nếu có anchor
        if (startPoint) nodes[0] = new RopeNode1(startPoint.position, true);
        if (endPoint) nodes[nodeCount - 1] = new RopeNode1(endPoint.position, true);

        if (line)
            line.positionCount = nodeCount;
    }

    // -------------------------------------------------------------------------
    // Simulation
    // -------------------------------------------------------------------------
    void FixedUpdate()
    {
        if (!simulate || nodes.Count == 0) return;

        SimulateVerlet(Time.fixedDeltaTime);
        ApplyConstraints();
        UpdateLineRenderer();
    }

    void SimulateVerlet(float dt)
    {
        Vector2 gravityVec = new Vector2(0, -gravity);

        for (int i = 0; i < nodeCount; i++)
        {
            if (nodes[i].isPinned) continue;

            RopeNode1 n = nodes[i];
            Vector2 pos = n.position;
            Vector2 old = n.oldPosition;
            Vector2 velocity = (pos - old) * damping;

            n.oldPosition = pos;
            n.position = pos + velocity + gravityVec * dt * dt;
            nodes[i] = n;
        }

        // Giữ anchor
        if (startPoint)
            nodes[0] = new RopeNode1(startPoint.position, true);
        if (endPoint)
            nodes[nodeCount - 1] = new RopeNode1(endPoint.position, true);
    }

    void ApplyConstraints()
    {
        for (int iter = 0; iter < constraintIterations; iter++)
        {
            for (int i = 0; i < nodeCount - 1; i++)
            {
                RopeNode1 n1 = nodes[i];
                RopeNode1 n2 = nodes[i + 1];

                Vector2 delta = n2.position - n1.position;
                float dist = delta.magnitude;
                if (dist == 0f) continue;

                float diff = (dist - segmentLength) / dist;
                Vector2 correction = delta * diff * 0.5f;

                if (!n1.isPinned) n1.position += correction;
                if (!n2.isPinned) n2.position -= correction;

                nodes[i] = n1;
                nodes[i + 1] = n2;
            }
        }

        // Cập nhật lại anchor chính xác
        if (startPoint) nodes[0] = new RopeNode1(startPoint.position, true);
        if (endPoint) nodes[nodeCount - 1] = new RopeNode1(endPoint.position, true);
    }

    void UpdateLineRenderer()
    {
        if (!line) return;
        for (int i = 0; i < nodeCount; i++)
            line.SetPosition(i, nodes[i].position);
    }

    // -------------------------------------------------------------------------
    // Cutting System
    // -------------------------------------------------------------------------
    public void CutAtNode(int index)
    {
        if (index <= 0 || index >= nodeCount - 1)
            return;

        // 1. Tách danh sách node
        List<RopeNode1> left = new List<RopeNode1>(nodes.GetRange(0, index + 1));
        List<RopeNode1> right = new List<RopeNode1>(nodes.GetRange(index, nodeCount - index));

        // 2. Đồng bộ vị trí cắt
        Vector2 cutPos = nodes[index].position;

        var lastLeft = left[left.Count - 1];
        lastLeft.position = cutPos;
        left[left.Count - 1] = lastLeft;

        var firstRight = right[0];
        firstRight.position = cutPos;
        right[0] = firstRight;

        // 3. Recoil
        Vector2 dir = (nodes[Mathf.Min(index + 1, nodeCount - 1)].position -
                       nodes[Mathf.Max(index - 1, 0)].position).normalized;
        ApplyRecoil(left, -dir * recoilStrength);
        ApplyRecoil(right, dir * recoilStrength);

        // 4. Tạo 2 dây mới
        SpawnRopePiece(left, "LeftPiece");
        SpawnRopePiece(right, "RightPiece");

        // 5. Xóa dây gốc
        Destroy(gameObject);
    }

    void ApplyRecoil(List<RopeNode1> list, Vector2 recoil)
    {
        for (int i = 0; i < list.Count; i++)
        {
            float falloff = (float)i / list.Count;
            RopeNode1 n = list[i];
            n.position += recoil * (1f - falloff) * 0.1f;
            list[i] = n;
        }
    }

    void SpawnRopePiece(List<RopeNode1> nodeList, string nameSuffix)
    {
        GameObject go = new GameObject("RopePiece_" + nameSuffix);
        var rope = go.AddComponent<VerletRope4>();
        var lr = go.GetComponent<LineRenderer>();
        rope.physicsProfile = physicsProfile;
        if (physicsProfile != null)
            rope.ApplyProfile();

        // Copy cài đặt visual
        if (line == null)
            line = GetComponent<LineRenderer>() ?? gameObject.AddComponent<LineRenderer>();
        CopyLineRendererSettings(line, lr);

        rope.line = lr;
        rope.nodeCount = nodeList.Count;
        rope.nodes = new List<RopeNode1>(nodeList);

        lr.positionCount = rope.nodeCount;
        rope.simulate = true;
        rope.startPoint = null;
        rope.endPoint = null;
    }

    // -------------------------------------------------------------------------
    // Debug / Utility
    // -------------------------------------------------------------------------
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
            CutAtNode(nodeCount / 2);
    }

    void CopyLineRendererSettings(LineRenderer src, LineRenderer dst)
    {
        if (!src || !dst) return;

        // --- Copy shared material ---
        if (src.sharedMaterial != null)
        {
            dst.sharedMaterial = src.sharedMaterial; // Giữ reference gốc, không tạo instance

            // Sao chép các property màu nếu shader có
            if (src.sharedMaterial.HasProperty("_Color"))
                dst.sharedMaterial.SetColor("_Color", src.sharedMaterial.GetColor("_Color"));
            else if (src.sharedMaterial.HasProperty("_BaseColor"))
                dst.sharedMaterial.SetColor("_BaseColor", src.sharedMaterial.GetColor("_BaseColor"));
            else if (src.sharedMaterial.HasProperty("_TintColor"))
                dst.sharedMaterial.SetColor("_TintColor", src.sharedMaterial.GetColor("_TintColor"));
        }

        // --- Copy MaterialPropertyBlock ---
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        src.GetPropertyBlock(block);
        dst.SetPropertyBlock(block);

        // --- Copy Gradient ---
        if (src.colorGradient != null)
        {
            Gradient srcGrad = src.colorGradient;
            GradientColorKey[] colorKeys = new GradientColorKey[srcGrad.colorKeys.Length];
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[srcGrad.alphaKeys.Length];
            System.Array.Copy(srcGrad.colorKeys, colorKeys, srcGrad.colorKeys.Length);
            System.Array.Copy(srcGrad.alphaKeys, alphaKeys, srcGrad.alphaKeys.Length);
            Gradient cloneGrad = new Gradient();
            cloneGrad.SetKeys(colorKeys, alphaKeys);
            dst.colorGradient = cloneGrad;
        }

        // --- Copy geometry & rendering ---
        dst.widthMultiplier = src.widthMultiplier;
        dst.startWidth = src.startWidth;
        dst.endWidth = src.endWidth;
        dst.startColor = src.startColor;
        dst.endColor = src.endColor;
        dst.textureMode = src.textureMode;
        dst.numCornerVertices = src.numCornerVertices;
        dst.numCapVertices = src.numCapVertices;
        dst.alignment = src.alignment;
        dst.shadowCastingMode = src.shadowCastingMode;
        dst.receiveShadows = src.receiveShadows;
        dst.sortingLayerID = src.sortingLayerID;
        dst.sortingOrder = src.sortingOrder;
        dst.useWorldSpace = src.useWorldSpace;
        dst.loop = src.loop;

        // --- Copy renderQueue ---
        if (src.sharedMaterial != null)
            dst.sharedMaterial.renderQueue = src.sharedMaterial.renderQueue;

        // --- Copy positions ---
        dst.positionCount = src.positionCount;
        Vector3[] temp = new Vector3[src.positionCount];
        src.GetPositions(temp);
        dst.SetPositions(temp);
    }



}
