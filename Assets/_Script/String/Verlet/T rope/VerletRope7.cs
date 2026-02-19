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

[DisallowMultipleComponent]
public class VerletRope7 : MonoBehaviour, IRopeDataProvider, ICuttableRope,
    IRopeRetractRelease, IRopeSimulationDataProvider
{
    [Header("Profile (Optional)")]
    public RopeProfile profile;

    [Header("Runtime (readonly)")]
    [SerializeField] private int nodeCount = 0;

    [SerializeField, HideInInspector] public float segmentLength = 0.2f;
    [SerializeField, HideInInspector] public float gravity = 9.81f;
    [SerializeField, HideInInspector, Range(0.8f, 0.9999f)] public float damping = 0.995f;
    [SerializeField, HideInInspector] public int constraintIterations = 6;
    [SerializeField, HideInInspector] public float slack = 1.0f;
    [SerializeField, HideInInspector] public float recoilStrength = 1.5f;
    [SerializeField, HideInInspector] public float weakRatio = 0.45f;

    [Header("Simulation flags")]
    public bool simulate = true;
    public bool isStartDetached = false;
    public bool isEndDetached = false;

    [Header("Render")]
    public Transform startPoint;
    public Transform endPoint;
    public bool shouldBlink = false;

    // core data
    public List<RopeNode4> nodes = new List<RopeNode4>();
    private readonly List<Vector2> nodePositions = new List<Vector2>();
    private RopeRender ropeRenderInstance;
    public int NodeCountRuntime => nodes != null ? nodes.Count : 0;

    // state
    public bool IsReady { get; private set; } = false;
    public bool isTaut = false;

    public bool IsTaut => this.isTaut;

    public event Action OnNodesReady;

    #region IRopeDataProvider (explicit)
    // Keep explicit implementations so public API doesn't get cluttered.
    int IRopeDataProvider.NodeCount => nodes?.Count ?? 0;
    IReadOnlyList<Vector2> IRopeDataProvider.NodesPositions => nodePositions;
    bool IRopeDataProvider.ShouldBlink => shouldBlink;
    event Action IRopeDataProvider.OnNodesReady
    {
        add { OnNodesReady += value; }
        remove { OnNodesReady -= value; }
    }
    #endregion

    #region ICuttableRope (explicit)
    bool ICuttableRope.CanBeCut =>
        nodes != null && nodes.Count >= 3 && nodes[0].isPinned && nodes[^1].isPinned;

    float ICuttableRope.GetStretch
    {
        get
        {
            float max = GetCurrentMaxLength();
            if (max <= Mathf.Epsilon) return 0f;
            return GetCurrentRopeLength() / max;
        }
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

    bool ICuttableRope.CutAt(int index)
    {
        if (index <= 0 || index >= nodes.Count - 1) return false;
        CutAtIndex(index);
        return true;
    }
    #endregion

    #region IRopeRetractRelease (explicit)
    IList<RopeNode4> IRopeRetractRelease.Nodes => nodes;
    float IRopeRetractRelease.SegmentLength => segmentLength;
    bool IRopeRetractRelease.IsReady => IsReady;
    #endregion

    #region IRopeSimulationDataProvider (explicit)
    // These were missing previously; implement them explicitly to satisfy the interface.
    IList<RopeNode4> IRopeSimulationDataProvider.Nodes => nodes;
    float IRopeSimulationDataProvider.Gravity => gravity;
    float IRopeSimulationDataProvider.Damping => damping;
    float IRopeSimulationDataProvider.SegmentLength => segmentLength;
    int IRopeSimulationDataProvider.ConstraintIterations => constraintIterations;
    bool IRopeSimulationDataProvider.Simulate => simulate;
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
        if (profile != null && profile.physics != null) ApplyProfile();
#endif
    }

    public void ApplyProfile()
    {
        if (profile == null || profile.physics == null) return;
        gravity = profile.physics.gravity;
        damping = profile.physics.damping;
        segmentLength = profile.physics.segmentLength;
        constraintIterations = profile.physics.constraintIterations;
        slack = profile.physics.slackFactor;
    }

    private void SetupRendererFromProfile()
{
    if (profile == null || profile.render == null)
        return;

    // 1) Reuse existing RopeRender if already exists in children
    if (ropeRenderInstance == null && TryGetExistingRopeRender(out var existing))
    {
        ropeRenderInstance = existing;
        ropeRenderInstance.Bind(this);
        return;
    }

    // 2) Spawn new render prefab if missing
    if (ropeRenderInstance == null)
    {
        var go = Instantiate(profile.render.renderPrefab, transform);

        if (!go.TryGetComponent(out ropeRenderInstance))
        {
            Debug.LogError($"[{name}] Render prefab does not contain RopeRender", go);
            Destroy(go);
            enabled = false;
            return;
        }

        ropeRenderInstance.Bind(this);
    }
    else
    {
        // 3) Ensure bound (in case profile changed / re-init)
        ropeRenderInstance.Bind(this);
    }
}


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

        // rebuild renderer if necessary
        if (ropeRenderInstance == null && profile != null && profile.render != null)
            SetupRendererFromProfile();

        simulate = true;
        MarkReady();
    }

    private void MarkReady()
    {
        if (IsReady) return;
        UpdateNodePositionsCache();
        IsReady = true;
        // initialize modules
        foreach (var module in GetComponents<IRopeModule>())
            module.Initialize(this);
        OnNodesReady?.Invoke();
    }

    private void UpdateNodePositionsCache()
    {
        nodePositions.Clear();
        if (nodes == null) return;
        for (int i = 0; i < nodes.Count; i++) nodePositions.Add(nodes[i].position);
    }

    private void Start()
    {
        if (!IsReady && nodes != null && nodes.Count >= 2)
            MarkReady();
    }

    private void FixedUpdate()
    {
        isTaut = ComputeIdealLength() * 1.135f < GetCurrentRopeLength();
        SyncAnchors();
        UpdateNodePositionsCache();
    }

    private void Update()
    {
        shouldBlink = GetCurrentRopeLength() >= GetCurrentMaxLength() / 1.25f;
    }

    #region Cutting & spawn helpers
    public void CutAtIndex(int index)
    {
        if (index <= 0 || index >= nodes.Count - 1) return;

        var left = new List<RopeNode4>(nodes.GetRange(0, index + 1));
        var right = new List<RopeNode4>(nodes.GetRange(index, nodes.Count - index));

        Vector2 cutPos = nodes[index].position;

        var lastLeft = left[left.Count - 1];
        lastLeft.position = cutPos; left[left.Count - 1] = lastLeft;
        var firstRight = right[0];
        firstRight.position = cutPos; right[0] = firstRight;

        Vector2 dir = Vector2.zero;
        int after = Mathf.Min(index + 1, nodes.Count - 1);
        int before = Mathf.Max(index - 1, 0);
        var diff = nodes[after].position - nodes[before].position;
        if (diff.sqrMagnitude > Mathf.Epsilon) dir = diff.normalized;

        ApplyRecoil(left, -dir * recoilStrength);
        ApplyRecoil(right, dir * recoilStrength);

        SpawnRopePiece(left, "LeftPiece", startPoint);
        SpawnRopePiece(right, "RightPiece", null, endPoint);

        Destroy(gameObject);
    }

    private void ApplyRecoil(List<RopeNode4> list, Vector2 recoil)
    {
        for (int i = 0; i < list.Count; i++)
        {
            float falloff = (float)i / list.Count;
            var n = list[i];
            n.position += recoil * (1f - falloff) * 0.1f;
            list[i] = n;
        }
    }

    private void SpawnRopePiece(List<RopeNode4> nodeList, string nameSuffix, Transform startAnchor = null, Transform endAnchor = null)
    {
        RopeFactory.CreateRope(profile, nodeList, startAnchor, endAnchor, null);
    }
    #endregion

    #region Length utils
    public float GetCurrentRopeLength()
    {
        if (nodes == null || nodes.Count < 2) return 0f;
        float total = 0f;
        for (int i = 0; i < nodes.Count - 1; i++)
            total += Vector2.Distance(nodes[i].position, nodes[i + 1].position);
        return total;
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
            var a = nodes[i];
            var b = nodes[i + 1];
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
                n.oldPosition = pos;
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
        OnNodesReady?.Invoke();
    }

    public float GetLengthAtStretch(float stretch01)
    {
        stretch01 = Mathf.Clamp01(stretch01);

        float ideal = ComputeIdealLength();
        float max = GetCurrentMaxLength();

        return Mathf.Lerp(ideal, max, stretch01);
    }

    private bool TryGetExistingRopeRender(out RopeRender existing)
    {
       existing = GetComponentInChildren<RopeRender>(true);
       return existing != null;
    }


}