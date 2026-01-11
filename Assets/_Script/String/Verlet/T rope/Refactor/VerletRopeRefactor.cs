using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class VerletRopeRefactor : MonoBehaviour
{
    [Header("Profile (Optional)")]
    public RopeProfile profile;

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
    [SerializeField] private float maxStretchDistance = 0.9f;   // Ngưỡng đứt do giãn
    [SerializeField] private float velocityThreshold = 0.02f;    // Ngưỡng tốc độ giãn
    [SerializeField] private float cutThreshold = 0.95f; // dây đạt 95% chiều dài tối đa thì bắt đầu xét
    [SerializeField] private float weakRatio = 0.45f;            // Tỷ lệ node yếu (giữa các node tự do)
    private bool hasCut = false;                                 // Cờ ngăn cắt lặp
    public bool isStartDetached = false;
    public bool isEndDetached = false;

    [SerializeField] private float retractSpeed = 5f;
    [SerializeField] private float releaseSpeed = 5f;
    [SerializeField] private KeyCode retractKey = KeyCode.W;
    [SerializeField] private KeyCode releaseKey = KeyCode.S;
    int index = 0;

    private int retractIndex = 1;
    [SerializeField] private int releaseIndex = 1;
    private bool isRetracting = false;
    private bool isReleasing = false;

    private float retractProgress = 0f;
    private float releaseProgress = 0f;

    [Header("Visual")]
    public Transform startPoint;
    public Transform endPoint;
    [SerializeField] private bool isBlinking = false;
    private Color originalColor = Color.magenta;
    private bool hasOriginalColor = false;
    private Color blinkColorA = Color.magenta;
    private Color blinkColorB = Color.white;
    private float blinkSpeed = 0.5f;
    private float blinkTimer = 0f;
    public bool shouldBlink = false;

    public LineRenderer line;
    public List<RopeNode4> nodes = new();
    public List<int> freeNodes = new List<int>();

    [SerializeField] private RopeRender ropeRenderPrefab;
    private RopeRender ropeRenderInstance;

    private void Awake()
    {
        if (profile.physics != null)
            ApplyProfile();
        else
        {
            Debug.LogError($"[{name}] Missing RopeProfile", this);
            enabled = false;
            return;
        }

        SetupRendererFromProfile();
    }

    void Start()
    {
        line = GetComponent<LineRenderer>();

        if (profile.physics != null)
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
            //InitializeRope(startPoint.position, endPoint.position, slack);
            InitializeRope_CompressStretch(startPoint.position, endPoint.position);
        }
        else
        {
            Debug.LogWarning($"[{name}] Rope missing startPoint or endPoint reference!");
        }
    }

    private void SetupRendererFromProfile()
    {
        if (profile.physics == null)
        {
            Debug.LogError($"[{name}] Missing RopePhysicsProfile", this);
            enabled = false;
            return;
        }

        if (profile.render == null)
        {
            Debug.LogError($"[{name}] RopeProfile missing render prefab", this);
            enabled = false;
            return;
        }

        GameObject go = Instantiate(profile.render.renderPrefab, transform);

        if (!go.TryGetComponent(out ropeRenderInstance))
        {
            Debug.LogError(
                $"[{name}] Render prefab does not contain RopeRender",
                go
            );
            Destroy(go);
            enabled = false;
            return;
        }

        //ropeRenderInstance.Bind(this);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (profile.physics != null)
            ApplyProfile();
    }
#endif

    public void ApplyProfile()
    {
        gravity = profile.physics.gravity;
        damping = profile.physics.damping;
        segmentLength = profile.physics.segmentLength;
        constraintIterations = profile.physics.constraintIterations;
        slack = profile.physics.slackFactor;
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
            nodes.Add(new RopeNode4(pos));
        }

        if (line)
            line.positionCount = nodeCount;

        var n = nodes[0];
        if (startPoint && !isStartDetached)
        {

            n.position = startPoint.position;
            n.oldPosition = startPoint.position;
            n.isPinned = true;
            nodes[0] = n;
        }
        else
        {
            n.isPinned = false;
            nodes[0] = n;
        }

        var nE = nodes[nodeCount - 1];
        if (endPoint && !isEndDetached)
        {
            nE.position = endPoint.position;
            nE.oldPosition = endPoint.position;
            nE.isPinned = true;
            nodes[nodeCount - 1] = nE;
        }
        else
        {
            nE.isPinned = false;
            nodes[0] = nE;
        }

        OnNodesReady?.Invoke();
        AttachRenderer();
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


        if (isRetracting)
            ApplyRetract(Time.fixedDeltaTime);
        else if (isReleasing)
            ApplyRelease(Time.fixedDeltaTime);
    }

    void SimulateVerlet(float dt)
    {
        Vector2 gravityVec = new Vector2(0, -gravity);

        for (int i = 0; i < nodeCount; i++)
        {
            if (nodes[i].isPinned) continue;

            RopeNode4 n = nodes[i];
            Vector2 pos = n.position;
            Vector2 old = n.oldPosition;
            Vector2 velocity = (pos - old) * damping;

            n.oldPosition = pos;
            n.position = pos + velocity + gravityVec * dt * dt;
            nodes[i] = n;
        }

        ForceUpdateAnchors();
    }

    void ApplyConstraints()
    {
        for (int iter = 0; iter < constraintIterations; iter++)
        {
            for (int i = 0; i < nodeCount - 1; i++)
            {
                RopeNode4 n1 = nodes[i];
                RopeNode4 n2 = nodes[i + 1];

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

        ForceUpdateAnchors();
    }

    void ForceUpdateAnchors()
    {
        if (startPoint)
            nodes[0] = new RopeNode4(startPoint.position, true);

        if (endPoint)
            nodes[nodeCount - 1] = new RopeNode4(endPoint.position, true);
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
        List<RopeNode4> left = new List<RopeNode4>(nodes.GetRange(0, index + 1));
        List<RopeNode4> right = new List<RopeNode4>(nodes.GetRange(index, nodeCount - index));

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
        SpawnRopePiece(left, "LeftPiece", startPoint);
        SpawnRopePiece(right, "RightPiece", null, endPoint);

        // 5. Xóa dây gốc
        Destroy(gameObject);
    }

    //void SpawnRopePiece(List<RopeNode4> nodeList, string nameSuffix, Transform startAnchor = null, Transform endAnchor = null)
    //{
    //    GameObject go = new GameObject("RopePiece_" + nameSuffix);
    //    var rope = go.AddComponent<VerletRope7>();
    //    var lr = go.GetComponent<LineRenderer>();
    //    rope.physicsProfile = physicsProfile;
    //    if (physicsProfile != null)
    //        rope.ApplyProfile();

    //    // Copy cài đặt visual
    //    if (line == null)
    //        line = GetComponent<LineRenderer>() ?? gameObject.AddComponent<LineRenderer>();
    //    CopyLineRendererSettings(line, lr);

    //    rope.line = lr;
    //    rope.nodeCount = nodeList.Count;
    //    rope.nodes = new List<RopeNode4>(nodeList);

    //    lr.positionCount = rope.nodeCount;
    //    rope.simulate = true;
    //    if(startAnchor != null)
    //        rope.startPoint = startAnchor;
    //    else
    //        rope.startPoint = null;

    //    if (endAnchor != null)
    //    {
    //        rope.endPoint = endAnchor;
    //    }
    //    else
    //    {
    //        rope.endPoint = null;
    //    }

    //    _onNodesReady?.Invoke();
    //    AttachRenderer();
    //}

    void ApplyRecoil(List<RopeNode4> list, Vector2 recoil)
    {
        for (int i = 0; i < list.Count; i++)
        {
            float falloff = (float)i / list.Count;
            RopeNode4 n = list[i];
            n.position += recoil * (1f - falloff) * 0.1f;
            list[i] = n;
        }
    }

    // -------------------------------------------------------------------------
    // Debug / Utility
    // -------------------------------------------------------------------------
    void Update()
    {
        // Giữ phím W để thu dây
        if (Input.GetKey(retractKey))
        {
            isRetracting = true;
            isReleasing = false;
        }
        else if (Input.GetKey(releaseKey))
        {
            isReleasing = true;
            isRetracting = false;
        }
        else
        {
            isRetracting = false;
            isReleasing = false;
        }

        // Cho phép thay đổi tốc độ động (ví dụ: Shift = nhanh gấp đôi)
        float speedModifier = Input.GetKey(KeyCode.LeftShift) ? 2f : 1f;
        retractSpeed = Mathf.Clamp(retractSpeed * speedModifier, 0.5f, 10f);
        releaseSpeed = Mathf.Clamp(releaseSpeed * speedModifier, 0.5f, 10f);

        //if (Input.GetKeyDown(KeyCode.C))
        //    CutAt(nodeCount / 2);


        shouldBlink = GetCurrentRopeLength() >= GetCurrentMaxLength() / 2.2f;

        if (shouldBlink)
        {
            UpdateBlink();
        }
        else
        {
            ResetToOriginalColor();
        }

        //TryAutoCutRope();
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

    private void TryAutoCutRope()
    {
        if (hasCut) return;
        if (nodes == null || nodes.Count < 3) return;
        if (!nodes.First().isPinned || !nodes.Last().isPinned) return;

        freeNodes = new List<int>();
        for (int i = 0; i < nodes.Count; i++)
            if (!nodes[i].isPinned)
                freeNodes.Add(i);

        if (freeNodes.Count == 0) return;

        int weakNodeIndex = freeNodes[
            Mathf.Clamp(
                Mathf.RoundToInt(freeNodes.Count * weakRatio),
                0,
                freeNodes.Count - 1
            )
        ];

        float current = GetCurrentRopeLength();
        float max = GetCurrentMaxLength();

        // Tính % stretch
        float stretch = current / max;

        // Chỉ cắt khi toàn dây đạt 92–97% tùy bạn tinh chỉnh
        if (stretch < 0.94f)
            return;

        hasCut = true;
        Debug.Log($"[AutoCut] Global overstretch | stretch={stretch:F3}");
        CutAtNode(weakNodeIndex);
    }

    public void StartRetract()
    {
        if (isReleasing) isReleasing = false;
        isRetracting = true;
    }

    public void StopRetract()
    {
        isRetracting = false;
    }

    public void StartRelease()
    {
        if (isRetracting) isRetracting = false;
        isReleasing = true;
    }

    public void StopRelease()
    {
        isReleasing = false;
    }



    private void ApplyRetract(float delta)
    {
        if (!isRetracting) return;

        index = FindLastPinnedCloseToAnchor(0.01f);
        Debug.Log("index " + index);
        retractIndex = ResolveRetractIndex(index);

        if (retractIndex < 1)
        {
            isRetracting = false;
            return;
        }

        RopeNode4 n = nodes[retractIndex];
        RopeNode4 prev = nodes[retractIndex - 1];

        Vector2 nextPos;
        if (retractIndex == nodeCount - 1)
        {
            nextPos = nodes[releaseIndex].position + Vector2.down;
        }
        else
        {
            nextPos = nodes[retractIndex + 1].position;
        }

        retractProgress = Vector2.Distance(n.position, prev.position) - retractSpeed * delta;
        Debug.Log("retractProgress " + retractProgress);

        n.isPinned = true;
        if (retractProgress <= 0f)
        {
            n.position = GeometryUtils.GetPointOnLine(prev.position, nextPos, 0f);
        }
        else
        {
            n.position = GeometryUtils.GetPointOnLine(prev.position, nextPos, retractProgress);
        }

        n.oldPosition = n.position;
        nodes[retractIndex] = n;
    }

    private void ApplyRelease(float delta)
    {
        Debug.Log("index r " + index);
        if (!isReleasing) return;

        index = FindLastPinnedCloseToAnchor(0.01f);
        Debug.Log("index r " + index);
        releaseIndex = ResolveReleaseIndex(index);

        if (releaseIndex <= 0)
        {
            isReleasing = false;
            return;
        }

        RopeNode4 n = nodes[releaseIndex];
        RopeNode4 prev = nodes[releaseIndex - 1];

        Vector2 nextPos;
        if (releaseIndex == nodeCount - 1)
        {
            nextPos = nodes[releaseIndex].position + Vector2.down;
        }
        else
        {
            nextPos = nodes[releaseIndex + 1].position;
        }

        // Tích lũy tiến trình nhả
        releaseProgress = Vector2.Distance(n.position, prev.position) + releaseSpeed * delta;

        Debug.Log("releaseProgress " + releaseProgress);
        Debug.Log("Distance " + Vector2.Distance(n.position, nextPos));

        if (releaseProgress >= segmentLength)
        {
            n.position = GeometryUtils.GetPointOnLine(prev.position, nextPos, segmentLength);
            n.isPinned = false;
        }
        else
        {
            n.position = GeometryUtils.GetPointOnLine(prev.position, nextPos, releaseProgress);
        }

        if (releaseIndex == nodeCount - 1)
        {
            n.isPinned = true;
        }
        n.oldPosition = n.position;
        nodes[releaseIndex] = n;
    }


    int FindLastPinnedCloseToAnchor(float threshold)
    {
        Vector2 anchorPos = nodes[0].position;

        for (int i = nodes.Count - 2; i >= 0; i--)
        {
            if (!nodes[i].isPinned)
                continue;

            float dist = Vector2.Distance(nodes[i].position, anchorPos);

            if (dist <= threshold)
                return i;
        }

        return -1;
    }

    int ResolveReleaseIndex(int index)
    {
        if (index <= 0 || index > nodes.Count - 1)
            return -1;

        if (index == nodes.Count - 1)
            return index;

        if (nodes[index + 1].isPinned)
        {
            return index + 1;
        }
        else
        {
            return index;
        }
    }

    int ResolveRetractIndex(int index)
    {
        if (index < 0 || index >= nodes.Count - 1)
            return -1;
        return index + 1;
    }

    public void InitializeRope_CompressStretch(Vector3 start, Vector3 end, int fixedNodeCount = 35)
    {
        nodes.Clear();

        nodeCount = Mathf.Max(2, fixedNodeCount);

        float idealTotalLength = segmentLength * (nodeCount - 1);   // độ dài lý tưởng
        float actualDist = Vector2.Distance(start, end);            // khoảng cách thực tế
        float scale = actualDist / idealTotalLength;                // hệ số nén/giãn

        Vector3 dir = (end - start).normalized;

        for (int i = 0; i < nodeCount; i++)
        {
            // vị trí spawn bị nén/giãn tạm thời
            float idealOffset = segmentLength * i;
            Vector3 pos = start + dir * (idealOffset * scale);
            nodes.Add(new RopeNode4(pos));
        }

        // Pin hai đầu nếu có anchor
        if (startPoint) nodes[0] = new RopeNode4(startPoint.position, true);
        if (endPoint) nodes[nodeCount - 1] = new RopeNode4(endPoint.position, true);

        if (line)
            line.positionCount = nodeCount;

        OnNodesReady?.Invoke();
        AttachRenderer();
    }

    private void UpdateBlink()
    {
        if (!isBlinking || line == null) return;

        // Tính toán màu blink theo thời gian vật lý (đồng bộ với FixedUpdate)
        Color blink = GetBlinkColor(blinkColorA, blinkColorB, blinkSpeed, Time.fixedTime);

        line.startColor = blink;
        line.endColor = blink;
    }

    private void CacheOriginalColor()
    {
        if (line == null)
        {
            originalColor = Color.magenta;
            hasOriginalColor = true;
            return;
        }

        // Ưu tiên lấy màu trực tiếp từ LineRenderer
        if (line.startColor != default)
            originalColor = line.startColor;
        else if (line.colorGradient != null && line.colorGradient.colorKeys.Length > 0)
            originalColor = line.colorGradient.colorKeys[0].color;
        else
            originalColor = Color.magenta;

        hasOriginalColor = true;
    }

    public void SetBlink(bool active, Color? colorA = null, Color? colorB = null, float speed = -1f)
    {
        isBlinking = active;

        if (!hasOriginalColor)
            CacheOriginalColor();

        blinkColorA = colorA ?? originalColor;
        blinkColorB = colorB ?? Color.white;

        if (speed > 0)
            blinkSpeed = speed;

        if (!isBlinking)
            ResetToOriginalColor();
    }

    private void ResetToOriginalColor()
    {
        if (!line) return;
        line.startColor = originalColor;
        line.endColor = originalColor;
    }

    public static Color GetBlinkColor(Color colorA, Color colorB, float blinkSpeed, float time)
    {
        float t = (Mathf.Sin(time * blinkSpeed) + 1f) * 0.5f;
        return Color.Lerp(colorA, colorB, t);
    }

    public float GetCurrentRopeLength()
    {
        if (nodes == null || nodes.Count < 2)
            return 0f;

        float totalLength = 0f;
        for (int i = 0; i < nodes.Count - 1; i++)
        {
            totalLength += Vector2.Distance(nodes[i].position, nodes[i + 1].position);
        }

        return totalLength;
    }

    public float GetLengthAtStretch(float stretchPercent)
    {
        float ideal = ComputeIdealLength();
        return ideal * (1f + stretchPercent);
    }

    public float GetCurrentMaxLength()
    {
        return ComputeIdealLength() * profile.physics.maxStretchFactor;
    }

    public float ComputeIdealLength()
    {
        float sum = 0f;

        for (int i = 0; i < nodes.Count - 1; i++)
        {
            RopeNode4 a = nodes[i];
            RopeNode4 b = nodes[i + 1];

            if (a.isPinned && b.isPinned)
            {
                // Nếu cả hai pinned → lấy độ dài thật ở runtime
                sum += Vector2.Distance(a.position, b.position);
            }
            else
            {
                // Nếu không → dùng segmentLength cố định
                sum += segmentLength;
            }
        }

        return sum;
    }

    public event Action OnNodesReady;

    private void AttachRenderer()
    {
        if (ropeRenderPrefab == null)
            ropeRenderPrefab = RopeDefaults.DefaultRenderPrefab;


        if (ropeRenderPrefab != null)
        {
            ropeRenderInstance = Instantiate(ropeRenderPrefab, transform);
        }
        else
        {
            Debug.LogWarning($"[{name}] No RopeRender prefab found. Creating runtime RopeRender.");

            GameObject go = new GameObject("RopeRender_Runtime");
            go.transform.SetParent(transform, false);

            go.AddComponent<LineRenderer>();
            ropeRenderInstance = go.AddComponent<RopeRender>();
        }

        //ropeRenderInstance.Bind(this);
    }

    public void FinalizeBuild()
    {
        AttachRenderer();
        OnNodesReady?.Invoke();
    }

    void SpawnRopePiece(
    List<RopeNode4> nodeList,
    string nameSuffix,
    Transform startAnchor = null,
    Transform endAnchor = null
)
    {
        GameObject go = new GameObject("RopePiece_" + nameSuffix);

        var newRope = go.AddComponent<VerletRope7>();
        newRope.profile.physics = profile.physics;

        if (profile.physics != null)
            newRope.ApplyProfile();

        // ---- DATA ONLY ----
        newRope.nodeCount = nodeList.Count;
        newRope.nodes = new List<RopeNode4>(nodeList);

        newRope.startPoint = startAnchor;
        newRope.endPoint = endAnchor;

        newRope.simulate = true;

        // ---- SINGLE LIFECYCLE ENTRY ----
        newRope.FinalizeBuild();
    }
}
