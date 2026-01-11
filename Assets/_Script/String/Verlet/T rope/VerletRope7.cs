//using UnityEngine;
//using System;
//using System.Collections.Generic;

//[System.Serializable]
//public struct RopeNode4
//{
//    public Vector2 position;
//    public Vector2 oldPosition;
//    public bool isPinned;
//    public RopeNode4(Vector2 pos, bool pinned = false)
//    {
//        position = pos;
//        oldPosition = pos;
//        isPinned = pinned;
//    }
//    public Vector2 Velocity => position - oldPosition;
//}

//public class VerletRope7 : MonoBehaviour, IRopeDataProvider, ICuttableRope, IRopeSimulationData, IRopeRetractRelease, IPendulumData
//{
//    [Header("Profile (Optional)")]
//    public RopeProfile profile;

//    [Header("Rope Parameters")]
//    public int nodeCount = 20;
//    [HideInInspector] public float segmentLength = 0.2f;
//    [HideInInspector] public float gravity = 9.81f;
//    [HideInInspector, Range(0.8f, 0.9999f)] public float damping = 0.995f;
//    [HideInInspector] public int constraintIterations = 6;
//    [HideInInspector] public float slack = 1.1f;
//    [HideInInspector] public float recoilStrength = 1.5f;
//    public float weakRatio = 0.45f;

//    [Header("Simulation")]
//    public bool simulate = true;
//    public bool isStartDetached = false;
//    public bool isEndDetached = false;

//    [Header("Visual")]
//    public Transform startPoint;
//    public Transform endPoint;
//    public bool shouldBlink = false;

//    public List<RopeNode4> nodes = new();
//    public List<int> freeNodes = new List<int>();
//    private readonly List<Vector2> nodePositions = new();
//    private RopeRender ropeRenderInstance;

//    public bool IsReady { get; private set; }
//    public bool isTaut = false;

//    int IRopeDataProvider.NodeCount => nodes?.Count ?? 0;
//    IReadOnlyList<Vector2> IRopeDataProvider.Nodes => nodePositions;

//    private void UpdateNodePositionsCache()
//    {
//        nodePositions.Clear();
//        for (int i = 0; i < nodes.Count; i++)
//            nodePositions.Add(nodes[i].position);
//    }

//    bool IRopeDataProvider.ShouldBlink => shouldBlink;

//    event Action IRopeDataProvider.OnNodesReady
//    {
//        add => _onNodesReady += value;
//        remove => _onNodesReady -= value;
//    }

//    private void NotifyNodesReady()
//    {
//        _onNodesReady?.Invoke();
//    }

//    private void Awake()
//    {
//        IsReady = false;
//        if (profile == null || profile.physics == null || profile.render == null)
//            return;
//        ApplyProfile();
//        SetupRendererFromProfile();
//    }

//    void Start()
//    {
//        if (!IsReady)
//            return;
//    }

//    private void MarkReady()
//    {
//        if (IsReady) return;
//        UpdateNodePositionsCache();
//        IsReady = true;
//        InitializeModules();
//        _onNodesReady?.Invoke();
//    }

//    public void InitializeRuntime(
//        RopeProfile sourceProfile,
//        List<RopeNode4> runtimeNodes,
//        Transform start,
//        Transform end)
//    {
//        if (runtimeNodes == null || runtimeNodes.Count < 2)
//        {
//            Debug.LogError($"[{name}] InitializeRuntime received invalid node list");
//            return;
//        }

//        profile = sourceProfile;
//        ApplyProfile();

//        nodes = new List<RopeNode4>(runtimeNodes);
//        nodeCount = nodes.Count;
//        startPoint = start;
//        endPoint = end;

//        SetupRendererFromProfile();
//        simulate = true;

//        MarkReady();
//    }

//    private void InitializeModules()
//    {
//        var modules = GetComponents<IRopeModule>();

//        foreach (var module in modules)
//        {
//            module.Initialize(this);
//        }
//    }

//    private void SetupRendererFromProfile()
//    {
//        if (profile == null || profile.physics == null)
//        {
//            Debug.LogError($"[{name}] Missing RopePhysicsProfile", this);
//            enabled = false;
//            return;
//        }

//        if (profile.render == null)
//        {
//            Debug.LogError($"[{name}] RopeProfile missing render prefab", this);
//            enabled = false;
//            return;
//        }

//        GameObject go = Instantiate(profile.render.renderPrefab, transform);
//        if (!go.TryGetComponent(out ropeRenderInstance))
//        {
//            Debug.LogError($"[{name}] Render prefab does not contain RopeRender", go);
//            Destroy(go);
//            enabled = false;
//            return;
//        }

//        ropeRenderInstance.Bind(this);
//    }

//#if UNITY_EDITOR
//    void OnValidate()
//    {
//        if (profile == null || profile.physics == null) return;
//        ApplyProfile();
//    }
//#endif

//    public void ApplyProfile()
//    {
//        if (profile == null || profile.physics == null) return;
//        gravity = profile.physics.gravity;
//        damping = profile.physics.damping;
//        segmentLength = profile.physics.segmentLength;
//        constraintIterations = profile.physics.constraintIterations;
//        slack = profile.physics.slackFactor;
//    }

//    // -------------------------------------------------------------------------
//    // Simulation tick
//    // (Verlet integration / constraints should be in RopeSimulation module if you separate it)
//    // -------------------------------------------------------------------------
//    void FixedUpdate()
//    {
//        if (ComputeIdealLength() < GetCurrentRopeLength())
//            isTaut = true;
//        else
//            isTaut = false;

//        SyncAnchors();
//        UpdateNodePositionsCache();
//    }

//    // -------------------------------------------------------------------------
//    // Cutting & Utility
//    // -------------------------------------------------------------------------
//    public void CutAtIndex(int index)
//    {
//        if (index <= 0 || index >= nodeCount - 1) return;

//        List<RopeNode4> left = new List<RopeNode4>(nodes.GetRange(0, index + 1));
//        List<RopeNode4> right = new List<RopeNode4>(nodes.GetRange(index, nodeCount - index));

//        Vector2 cutPos = nodes[index].position;
//        var lastLeft = left[left.Count - 1];
//        lastLeft.position = cutPos;
//        left[left.Count - 1] = lastLeft;
//        var firstRight = right[0];
//        firstRight.position = cutPos;
//        right[0] = firstRight;

//        Vector2 dir = (nodes[Mathf.Min(index + 1, nodeCount - 1)].position -
//                       nodes[Mathf.Max(index - 1, 0)].position).normalized;

//        ApplyRecoil(left, -dir * recoilStrength);
//        ApplyRecoil(right, dir * recoilStrength);

//        SpawnRopePiece(left, "LeftPiece", startPoint);
//        SpawnRopePiece(right, "RightPiece", null, endPoint);

//        Destroy(gameObject);
//    }

//    int ICuttableRope.RecommendedCutIndex
//    {
//        get
//        {
//            var free = new List<int>();
//            for (int i = 1; i < nodes.Count - 1; i++)
//                if (!nodes[i].isPinned)
//                    free.Add(i);
//            if (free.Count == 0) return -1;
//            return free[Mathf.Clamp(Mathf.RoundToInt(free.Count * weakRatio), 0, free.Count - 1)];
//        }
//    }

//    bool ICuttableRope.CanBeCut =>
//        nodes != null &&
//        nodes.Count >= 3 &&
//        nodes[0].isPinned &&
//        nodes[^1].isPinned;

//    float ICuttableRope.GetStretch
//    {
//        get
//        {
//            float max = GetCurrentMaxLength();
//            if (max <= Mathf.Epsilon) return 0f;
//            return GetCurrentRopeLength() / max;
//        }
//    }

//    IList<RopeNode4> IRopeSimulationData.Nodes => nodes;
//    float IRopeSimulationData.Gravity => gravity;
//    float IRopeSimulationData.Damping => damping;
//    float IRopeSimulationData.SegmentLength => segmentLength;
//    int IRopeSimulationData.ConstraintIterations => constraintIterations;
//    bool IRopeSimulationData.Simulate => simulate;

//    IList<RopeNode4> IRopeRetractRelease.Nodes => nodes;
//    float IRopeRetractRelease.SegmentLength => segmentLength;
//    bool IRopeRetractRelease.IsReady => this.IsReady;

//    bool IPendulumData.IsReady => this.IsReady;

//    Transform IPendulumData.Anchor => this.startPoint;

//    Rigidbody2D IPendulumData.Bob
//    {
//        get
//        {
//            if (endPoint == null) return null;

//            if (endPoint.GetComponent<Rigidbody2D>() == null) return null;

//            if (endPoint.GetComponent<Rigidbody2D>()) return endPoint.GetComponent<Rigidbody2D>();

//            return null;
//        }
//    }

//    float IPendulumData.IdealLength
//    {
//        get
//        {
//            return ComputeIdealLength();
//        }
//    }

//    int IPendulumData.Substeps
//    {
//        get
//        {
//            if (profile == null || profile.physics == null)
//                return 1;

//            return Mathf.Max(1, profile.physics.substeps);
//        }
//    }

//    public RopePhysicsProfile PendulumPhysics => this.profile.physics;

//    bool IRopeSimulationData.IsReady => this.IsReady;

//    bool IRopeSimulationData.IsTaut => this.isTaut;

//    float IRopeSimulationData.CurentLength => this.GetCurrentRopeLength();

//    float IRopeSimulationData.IdealLength => this.ComputeIdealLength();

//    bool ICuttableRope.CutAt(int index)
//    {
//        if (index <= 0 || index >= nodes.Count - 1) return false;
//        CutAtIndex(index);
//        return true;
//    }

//    void ApplyRecoil(List<RopeNode4> list, Vector2 recoil)
//    {
//        for (int i = 0; i < list.Count; i++)
//        {
//            float falloff = (float)i / list.Count;
//            RopeNode4 n = list[i];
//            n.position += recoil * (1f - falloff) * 0.1f;
//            list[i] = n;
//        }
//    }

//    void Update()
//    {
//        shouldBlink = GetCurrentRopeLength() >= GetCurrentMaxLength() / 1.25f;
//    }

//    public float GetCurrentRopeLength()
//    {
//        if (nodes == null || nodes.Count < 2) return 0f;
//        float totalLength = 0f;
//        for (int i = 0; i < nodes.Count - 1; i++)
//            totalLength += Vector2.Distance(nodes[i].position, nodes[i + 1].position);
//        return totalLength;
//    }

//    public float GetLengthAtStretch(float stretchPercent)
//    {
//        float ideal = ComputeIdealLength();
//        return ideal * (1f + stretchPercent);
//    }

//    public float GetCurrentMaxLength()
//    {
//        return ComputeIdealLength() * (profile != null && profile.physics != null ? profile.physics.maxStretchFactor : 1f);
//    }

//    public float ComputeIdealLength()
//    {
//        float sum = 0f;
//        for (int i = 0; i < nodes.Count - 1; i++)
//        {
//            RopeNode4 a = nodes[i];
//            RopeNode4 b = nodes[i + 1];
//            if (a.isPinned && b.isPinned)
//                sum += Vector2.Distance(a.position, b.position);
//            else
//                sum += segmentLength;
//        }
//        return sum;
//    }

//    public event Action _onNodesReady;

//    public void FinalizeBuild()
//    {
//        MarkReady();
//        NotifyNodesReady();
//    }

//    void SpawnRopePiece(List<RopeNode4> nodeList, string nameSuffix, Transform startAnchor = null, Transform endAnchor = null)
//    {
//        RopeFactory.CreateRope(profile, nodeList, startAnchor, endAnchor, gameObject);
//    }

//    private void SyncAnchors()
//    {
//        if (startPoint && !isStartDetached && nodes.Count > 0)
//        {
//            var n = nodes[0];
//            Vector2 pos = startPoint.position;
//            if (n.position != pos)
//            {
//                n.position = pos;
//                n.oldPosition = pos; // reset velocity
//                n.isPinned = true;
//                nodes[0] = n;
//            }
//        }

//        if (endPoint && !isEndDetached && nodes.Count > 1)
//        {
//            int last = nodes.Count - 1;
//            var n = nodes[last];
//            Vector2 pos = endPoint.position;
//            if (n.position != pos)
//            {
//                n.position = pos;
//                n.oldPosition = pos;
//                n.isPinned = true;
//                nodes[last] = n;
//            }
//        }
//    }
//}

using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct RopeNode4
{
    public Vector2 position;
    public Vector2 oldPosition;
    public bool isPinned;
    public RopeNode4(Vector2 pos, bool pinned = false)
    {
        position = pos;
        oldPosition = pos;
        isPinned = pinned;
    }
    public Vector2 Velocity => position - oldPosition;
}

/// <summary>
/// VerletRope7 (cleaned)
/// - Chỉ lo position-based rope: nodes, profile application, cutting, renderer binding.
/// - Không implement IPendulumData nữa (không ép Verlet trở thành nguồn dữ liệu cho module force-based).
/// - Simulation (Verlet integration + constraint relaxation) nên đặt vào một module riêng (RopeSimulationVerlet).
/// - Giữ public API tối thiểu mà renderer / factory / cut system cần.
/// </summary>
[DisallowMultipleComponent]
public class VerletRope7 : MonoBehaviour, IRopeDataProvider, ICuttableRope, IRopeSimulationData, IRopeRetractRelease
{
    [Header("Profile (Optional)")]
    public RopeProfile profile;

    [Header("Rope Parameters (runtime from profile)")]
    [Tooltip("Number of nodes (informational). Actual node list drives behavior.")]
    public int nodeCount = 0;

    [SerializeField, HideInInspector] public float segmentLength = 0.2f;
    [SerializeField, HideInInspector] public float gravity = 9.81f;
    [SerializeField, HideInInspector, Range(0.8f, 0.9999f)] public float damping = 0.995f;
    [SerializeField, HideInInspector] public int constraintIterations = 6;
    [SerializeField, HideInInspector] public float slack = 1.1f;
    [SerializeField, HideInInspector] public float recoilStrength = 1.5f;
    [SerializeField, HideInInspector] public float weakRatio = 0.45f;

    [Header("Simulation")]
    public bool simulate = true;
    public bool isStartDetached = false;
    public bool isEndDetached = false;

    [Header("Visual")]
    public Transform startPoint;
    public Transform endPoint;
    public bool shouldBlink = false;

    // Core data
    public List<RopeNode4> nodes = new List<RopeNode4>();
    public List<int> freeNodes = new List<int>();

    // Lightweight cache for renderer / consumers that read positions
    private readonly List<Vector2> nodePositions = new List<Vector2>();
    private RopeRender ropeRenderInstance;

    // State
    public bool IsReady { get; private set; } = false;
    public bool isTaut = false;

    // Event for consumers waiting for nodes to be ready
    public event Action _onNodesReady;

    #region IRopeDataProvider (minimal)
    int IRopeDataProvider.NodeCount => nodes?.Count ?? 0;
    IReadOnlyList<Vector2> IRopeDataProvider.Nodes => nodePositions;
    bool IRopeDataProvider.ShouldBlink => shouldBlink;
    event Action IRopeDataProvider.OnNodesReady
    {
        add => _onNodesReady += value;
        remove => _onNodesReady -= value;
    }
    #endregion

    #region IRopeSimulationData & IRopeRetractRelease
    // Expose Verlet-specific properties expected by Verlet simulation modules
    IList<RopeNode4> IRopeSimulationData.Nodes => nodes;
    float IRopeSimulationData.Gravity => gravity;
    float IRopeSimulationData.Damping => damping;
    float IRopeSimulationData.SegmentLength => segmentLength;
    int IRopeSimulationData.ConstraintIterations => constraintIterations;
    bool IRopeSimulationData.Simulate => simulate;
    bool IRopeSimulationData.IsReady => IsReady;
    bool IRopeSimulationData.IsTaut => isTaut;
    float IRopeSimulationData.CurentLength => GetCurrentRopeLength();
    float IRopeSimulationData.IdealLength => ComputeIdealLength();

    IList<RopeNode4> IRopeRetractRelease.Nodes => nodes;
    float IRopeRetractRelease.SegmentLength => segmentLength;
    bool IRopeRetractRelease.IsReady => IsReady;
    #endregion

    private void Awake()
    {
        IsReady = false;
        if (profile == null || profile.physics == null || profile.render == null)
            return;

        ApplyProfile();
        SetupRendererFromProfile();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (profile != null && profile.physics != null)
            ApplyProfile();
#endif
    }

    /// <summary>
    /// Apply profile values relevant to Verlet system.
    /// </summary>
    public void ApplyProfile()
    {
        if (profile == null || profile.physics == null) return;

        gravity = profile.physics.gravity;
        damping = profile.physics.damping;
        segmentLength = profile.physics.segmentLength;
        constraintIterations = profile.physics.constraintIterations;
        slack = profile.physics.slackFactor;
        // keep recoilStrength / weakRatio as previously serialized values (optional to expose in profile)
    }

    private void SetupRendererFromProfile()
    {
        if (profile == null || profile.physics == null)
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
            Debug.LogError($"[{name}] Render prefab does not contain RopeRender", go);
            Destroy(go);
            enabled = false;
            return;
        }

        ropeRenderInstance.Bind(this);
    }

    /// <summary>
    /// Initialize rope at runtime with an explicit node list.
    /// </summary>
    public void InitializeRuntime(RopeProfile sourceProfile, List<RopeNode4> runtimeNodes, Transform start, Transform end)
    {
        if (runtimeNodes == null || runtimeNodes.Count < 2)
        {
            Debug.LogError($"[{name}] InitializeRuntime received invalid node list");
            return;
        }

        profile = sourceProfile;
        ApplyProfile();

        nodes = new List<RopeNode4>(runtimeNodes);
        nodeCount = nodes.Count;
        startPoint = start;
        endPoint = end;

        SetupRendererFromProfile();
        simulate = true;

        MarkReady();
    }

    private void MarkReady()
    {
        if (IsReady) return;
        UpdateNodePositionsCache();
        IsReady = true;
        InitializeModules();
        _onNodesReady?.Invoke();
    }

    private void InitializeModules()
    {
        var modules = GetComponents<IRopeModule>();
        foreach (var module in modules)
        {
            module.Initialize(this);
        }
    }

    private void UpdateNodePositionsCache()
    {
        nodePositions.Clear();
        if (nodes == null) return;
        for (int i = 0; i < nodes.Count; i++)
            nodePositions.Add(nodes[i].position);
    }

    void Start()
    {
        // If you create nodes dynamically, call FinalizeBuild() to mark ready.
        if (!IsReady && nodes != null && nodes.Count >= 2)
        {
            nodeCount = nodes.Count;
            MarkReady();
        }
    }

    void FixedUpdate()
    {
        // Taut check is cheap; keep it here to allow other modules to react
        isTaut = ComputeIdealLength() < GetCurrentRopeLength();

        // Synchronize pinned anchors (start/end) with external transforms
        SyncAnchors();

        // Update cache for renderer / external readers
        UpdateNodePositionsCache();
    }

    void Update()
    {
        shouldBlink = GetCurrentRopeLength() >= GetCurrentMaxLength() / 1.25f;
    }

    #region Cutting & Utility
    public void CutAtIndex(int index)
    {
        if (index <= 0 || index >= nodes.Count - 1) return;

        List<RopeNode4> left = new List<RopeNode4>(nodes.GetRange(0, index + 1));
        List<RopeNode4> right = new List<RopeNode4>(nodes.GetRange(index, nodes.Count - index));

        Vector2 cutPos = nodes[index].position;

        // ensure endpoints coincide exactly at cut
        var lastLeft = left[left.Count - 1];
        lastLeft.position = cutPos;
        left[left.Count - 1] = lastLeft;

        var firstRight = right[0];
        firstRight.position = cutPos;
        right[0] = firstRight;

        // recoil impulse direction uses neighbor positions if available
        Vector2 dir = Vector2.zero;
        int after = Mathf.Min(index + 1, nodes.Count - 1);
        int before = Mathf.Max(index - 1, 0);
        Vector2 a = nodes[after].position;
        Vector2 b = nodes[before].position;
        Vector2 diff = a - b;
        if (diff.sqrMagnitude > Mathf.Epsilon) dir = diff.normalized;

        ApplyRecoil(left, -dir * recoilStrength);
        ApplyRecoil(right, dir * recoilStrength);

        SpawnRopePiece(left, "LeftPiece", startPoint);
        SpawnRopePiece(right, "RightPiece", null, endPoint);

        Destroy(gameObject);
    }

    int ICuttableRope.RecommendedCutIndex
    {
        get
        {
            var free = new List<int>();
            for (int i = 1; i < nodes.Count - 1; i++)
                if (!nodes[i].isPinned)
                    free.Add(i);
            if (free.Count == 0) return -1;
            return free[Mathf.Clamp(Mathf.RoundToInt(free.Count * weakRatio), 0, free.Count - 1)];
        }
    }

    bool ICuttableRope.CanBeCut =>
        nodes != null &&
        nodes.Count >= 3 &&
        nodes[0].isPinned &&
        nodes[^1].isPinned;

    float ICuttableRope.GetStretch
    {
        get
        {
            float max = GetCurrentMaxLength();
            if (max <= Mathf.Epsilon) return 0f;
            return GetCurrentRopeLength() / max;
        }
    }

    bool ICuttableRope.CutAt(int index)
    {
        if (index <= 0 || index >= nodes.Count - 1) return false;
        CutAtIndex(index);
        return true;
    }

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

    void SpawnRopePiece(List<RopeNode4> nodeList, string nameSuffix, Transform startAnchor = null, Transform endAnchor = null)
    {
        RopeFactory.CreateRope(profile, nodeList, startAnchor, endAnchor, gameObject);
    }
    #endregion

    #region Length / Geometry helpers
    public float GetCurrentRopeLength()
    {
        if (nodes == null || nodes.Count < 2) return 0f;
        float totalLength = 0f;
        for (int i = 0; i < nodes.Count - 1; i++)
            totalLength += Vector2.Distance(nodes[i].position, nodes[i + 1].position);
        return totalLength;
    }

    public float GetLengthAtStretch(float stretchPercent)
    {
        float ideal = ComputeIdealLength();
        return ideal * (1f + stretchPercent);
    }

    public float GetCurrentMaxLength()
    {
        return ComputeIdealLength() * (profile != null && profile.physics != null ? profile.physics.maxStretchFactor : 1f);
    }

    public float ComputeIdealLength()
    {
        if (nodes == null || nodes.Count < 2) return 0f;
        float sum = 0f;
        for (int i = 0; i < nodes.Count - 1; i++)
        {
            RopeNode4 a = nodes[i];
            RopeNode4 b = nodes[i + 1];
            if (a.isPinned && b.isPinned)
                sum += Vector2.Distance(a.position, b.position);
            else
                sum += segmentLength;
        }
        return sum;
    }
    #endregion

    private void SyncAnchors()
    {
        if (startPoint && !isStartDetached && nodes.Count > 0)
        {
            var n = nodes[0];
            Vector2 pos = startPoint.position;
            if (n.position != pos)
            {
                n.position = pos;
                n.oldPosition = pos; // reset velocity for pinned node
                n.isPinned = true;
                nodes[0] = n;
            }
        }

        if (endPoint && !isEndDetached && nodes.Count > 1)
        {
            int last = nodes.Count - 1;
            var n = nodes[last];
            Vector2 pos = endPoint.position;
            if (n.position != pos)
            {
                n.position = pos;
                n.oldPosition = pos;
                n.isPinned = true;
                nodes[last] = n;
            }
        }
    }

    public void FinalizeBuild()
    {
        MarkReady();
        _onNodesReady?.Invoke();
    }
}