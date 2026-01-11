using UnityEngine;
using System.Collections;

/// <summary>
/// RopeVerletFull_Modified
/// - Verlet rope
/// - Iterative constraints
/// - Force-based bridge to Rigidbody2D
/// - Split/cut support; option to make mid-point after split a free node (no anchor)
/// - Preserves per-node velocity when splitting
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class RopeVerletFull_Modified : MonoBehaviour
{
    [Header("Anchors")]
    public Transform startPoint;
    public Transform endPointTransform;
    public Rigidbody2D endRigidbody; // if assigned, bridge applies to this

    [Header("Rope geometry")]
    [Min(2)] public int nodeCount = 20;
    public float ropeLength = 4f;
    public int constraintIterations = 20;
    [Tooltip("per-segment maximum allowed stretch factor (>=1.0)")]
    public float maxSegmentStretch = 1.05f;

    [Header("Integration & damping")]
    [Range(0.8f, 1.0f)] public float damping = 0.99f;
    public float gravity = -9.81f;
    public float maxNodeSpeed = 50f;

    [Header("Bridge (Verlet -> Rigidbody)")]
    public float bridgeK = 150f;
    public float bridgeDamping = 0.12f;
    [Range(0f, 1f)] public float bridgeStiffness = 0.85f;
    public float maxBridgeForce = 60f;

    [Header("Break / Cut settings")]
    public bool autoBreakEnabled = true;
    public bool breakDetectionEnabled = true;
    [Tooltip("If a segment length > segmentLength * breakThresholdFactor, it will be considered over-stretched")]
    public float breakThresholdFactor = 1.5f;
    public bool singleUseBreak = true;

    [Tooltip("If true, mid point after a split will be a free node (no anchor). Recommended for your case.")]
    public bool midAnchorIsFree = true; // <-- your requested behavior (default true)

    [Tooltip("If true and midAnchorIsFree==false, mid anchor becomes Rigidbody2D instead")]
    public bool midAnchorHasRigidbody = false;
    public float midAnchorMass = 1f;

    [Header("Visual/Debug")]
    public LineRenderer lineRenderer;
    public bool drawGizmosNodes = false;
    public float nodeGizmoSize = 0.03f;

    // internals
    private Vector2[] positions;
    private Vector2[] oldPositions;
    private float segmentLength;
    private int lastIndex;
    private float lastFixedDt = 0.02f;

    void Awake()
    {
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        InitializeNodes();
    }

    void OnValidate()
    {
        nodeCount = Mathf.Max(2, nodeCount);
        ropeLength = Mathf.Max(0.0001f, ropeLength);
        constraintIterations = Mathf.Max(1, constraintIterations);
        maxSegmentStretch = Mathf.Max(1f, maxSegmentStretch);
        if (Application.isPlaying) InitializeNodes();
    }

    void InitializeNodes()
    {
        segmentLength = Mathf.Max(0.0001f, ropeLength / (nodeCount - 1));
        positions = new Vector2[nodeCount];
        oldPositions = new Vector2[nodeCount];
        lastIndex = nodeCount - 1;

        Vector2 a = (startPoint != null) ? (Vector2)startPoint.position : Vector2.zero;
        Vector2 b;
        if (endRigidbody != null) b = endRigidbody.position;
        else if (endPointTransform != null) b = endPointTransform.position;
        else b = a + Vector2.down * ropeLength;

        for (int i = 0; i < nodeCount; i++)
        {
            float t = (float)i / lastIndex;
            Vector2 p = Vector2.Lerp(a, b, t);
            positions[i] = p;
            oldPositions[i] = p;
        }

        if (lineRenderer != null) lineRenderer.positionCount = nodeCount;
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        if (dt <= 0f) return;
        lastFixedDt = dt;

        SimulateVerlet(dt);
        for (int i = 0; i < constraintIterations; i++) ApplyConstraints();
        ApplyBridgeForce(dt);

        if (autoBreakEnabled && breakDetectionEnabled)
        {
            int seg = DetectBreakSegment();
            if (seg >= 0)
            {
                // split at segment index seg
                SplitAtSegment(seg);
                if (singleUseBreak) Destroy(gameObject);
            }
        }
    }

    void LateUpdate()
    {
        DrawRope();
    }

    void SimulateVerlet(float dt)
    {
        Vector2 g = Vector2.up * gravity;
        for (int i = 1; i <= lastIndex; i++)
        {
            Vector2 pos = positions[i];
            Vector2 old = oldPositions[i];
            Vector2 vel = (pos - old) * damping;
            if (vel.magnitude > maxNodeSpeed) vel = vel.normalized * maxNodeSpeed;
            oldPositions[i] = pos;
            positions[i] = pos + vel + g * dt * dt;
        }
    }

    void ApplyConstraints()
    {
        if (startPoint != null) positions[0] = startPoint.position;

        for (int i = 0; i < lastIndex; i++)
        {
            Vector2 p1 = positions[i];
            Vector2 p2 = positions[i + 1];
            Vector2 delta = p2 - p1;
            float dist = delta.magnitude;
            if (dist <= 1e-6f) continue;

            float maxAllowed = segmentLength * maxSegmentStretch;
            float target = segmentLength;
            if (dist > maxAllowed) target = maxAllowed;

            float diff = (dist - target) / dist;
            Vector2 corr = delta * diff;

            bool firstFixed = (i == 0);
            bool secondEdge = (i + 1 == lastIndex);

            if (firstFixed && !secondEdge)
            {
                positions[i + 1] = p2 - corr;
            }
            else
            {
                positions[i] = p1 + corr * 0.5f;
                positions[i + 1] = p2 - corr * 0.5f;
            }
        }

        if (startPoint != null) positions[0] = startPoint.position;
        if (endRigidbody == null && endPointTransform != null) positions[lastIndex] = endPointTransform.position;
    }

    void ApplyBridgeForce(float dt)
    {
        if (endRigidbody == null) return;
        if (dt <= 0f) return;

        Vector2 nodeBeforeEnd = positions[lastIndex - 1];
        Vector2 rbPos = endRigidbody.position;

        Vector2 delta = nodeBeforeEnd - rbPos;
        float dist = delta.magnitude;
        if (dist <= 1e-6f) return;

        Vector2 n = delta / dist;
        float desiredDist = segmentLength;
        float maxAllowed = segmentLength * maxSegmentStretch;
        if (dist > maxAllowed) desiredDist = maxAllowed;

        float stretch = dist - desiredDist;
        if (stretch <= 0f) return;

        float k = Mathf.Max(0f, bridgeK);
        float springMag = k * stretch;

        Vector2 rbVel = endRigidbody.linearVelocity;
        Vector2 nodeVel = (positions[lastIndex] - oldPositions[lastIndex]) / Mathf.Max(lastFixedDt, 1e-6f);
        float relVelAlong = Vector2.Dot(rbVel - nodeVel, n);

        float c = Mathf.Max(0f, bridgeDamping);
        float dampMag = c * relVelAlong;

        Vector2 forceOnRb = n * (springMag + dampMag);
        if (forceOnRb.magnitude > maxBridgeForce) forceOnRb = forceOnRb.normalized * maxBridgeForce;

        endRigidbody.AddForce(forceOnRb, ForceMode2D.Force);

        float nodeMoveFactor = 0.5f * (1f - Mathf.Clamp01(bridgeStiffness));
        positions[lastIndex] -= n * (stretch * nodeMoveFactor);

        Vector2 lastVel = (positions[lastIndex] - oldPositions[lastIndex]) / Mathf.Max(lastFixedDt, 1e-6f);
        if (lastVel.magnitude > maxNodeSpeed)
        {
            Vector2 clamped = lastVel.normalized * maxNodeSpeed;
            oldPositions[lastIndex] = positions[lastIndex] - clamped * lastFixedDt;
        }
    }

    int DetectBreakSegment()
    {
        float threshold = segmentLength * breakThresholdFactor;
        for (int i = 0; i < lastIndex; i++)
        {
            float d = (positions[i + 1] - positions[i]).magnitude;
            if (d > threshold) return i;
        }
        return -1;
    }

    // ===== SPLIT: modified behavior: when midAnchorIsFree == true we DO NOT create a mid anchor.
    public void SplitAtSegment(int segmentIndex)
    {
        if (segmentIndex < 0 || segmentIndex >= lastIndex) return;

        // gather per-node velocity
        Vector2[] vel = new Vector2[positions.Length];
        for (int i = 0; i < positions.Length; i++)
            vel[i] = (positions[i] - oldPositions[i]) / Mathf.Max(lastFixedDt, 1e-6f);

        // left: nodes 0 .. segmentIndex
        int leftCount = segmentIndex + 1;
        Vector2[] leftPos = new Vector2[leftCount];
        Vector2[] leftVel = new Vector2[leftCount];
        for (int i = 0; i <= segmentIndex; i++) { leftPos[i] = positions[i]; leftVel[i] = vel[i]; }

        // right: nodes segmentIndex+1 .. lastIndex
        int rightCount = lastIndex - segmentIndex;
        Vector2[] rightPos = new Vector2[rightCount];
        Vector2[] rightVel = new Vector2[rightCount];
        for (int i = segmentIndex + 1, j = 0; i <= lastIndex; i++, j++) { rightPos[j] = positions[i]; rightVel[j] = vel[i]; }

        // if midAnchorIsFree: DO NOT create a mid transform - fragments will have free ends at the cut
        if (midAnchorIsFree)
        {
            // left fragment: start = original startPoint, end = NONE (free)
            GameObject leftGO = new GameObject(name + "_fragL");
            CopyLineRenderer(leftGO);
            RopeVerletFull_Modified leftRope = leftGO.AddComponent<RopeVerletFull_Modified>();
            CopyConfigTo(leftRope);
            leftRope.startPoint = this.startPoint;
            leftRope.endPointTransform = null;
            leftRope.endRigidbody = null;
            leftRope.InitializeFromPositions(leftPos, leftVel);

            // right fragment: start = NONE (free), end = original end (rigidbody or transform)
            GameObject rightGO = new GameObject(name + "_fragR");
            CopyLineRenderer(rightGO);
            RopeVerletFull_Modified rightRope = rightGO.AddComponent<RopeVerletFull_Modified>();
            CopyConfigTo(rightRope);
            rightRope.startPoint = null;
            rightRope.endPointTransform = this.endPointTransform;
            rightRope.endRigidbody = this.endRigidbody;
            rightRope.InitializeFromPositions(rightPos, rightVel);

            leftGO.transform.parent = transform.parent;
            rightGO.transform.parent = transform.parent;

            StartCoroutine(DisableBreakForFrames(leftRope, 3));
            StartCoroutine(DisableBreakForFrames(rightRope, 3));
        }
        else
        {
            // previous behavior: create mid anchor (transform or rigidbody)
            Vector2 midPos = (positions[segmentIndex] + positions[segmentIndex + 1]) * 0.5f;
            GameObject midGO = new GameObject(name + "_mid");
            midGO.transform.position = midPos;
            Transform midT = midGO.transform;
            Rigidbody2D midRb = null;
            if (midAnchorHasRigidbody)
            {
                midRb = midGO.AddComponent<Rigidbody2D>();
                midRb.mass = Mathf.Max(0.0001f, midAnchorMass);
                midRb.linearVelocity = 0.5f * (vel[segmentIndex] + vel[segmentIndex + 1]);
            }

            GameObject leftGO = new GameObject(name + "_fragL");
            CopyLineRenderer(leftGO);
            RopeVerletFull_Modified leftRope = leftGO.AddComponent<RopeVerletFull_Modified>();
            CopyConfigTo(leftRope);
            leftRope.startPoint = this.startPoint;
            leftRope.endPointTransform = midT;
            leftRope.endRigidbody = null;
            leftRope.InitializeFromPositions(leftPos, leftVel);

            GameObject rightGO = new GameObject(name + "_fragR");
            CopyLineRenderer(rightGO);
            RopeVerletFull_Modified rightRope = rightGO.AddComponent<RopeVerletFull_Modified>();
            CopyConfigTo(rightRope);
            rightRope.startPoint = midT;
            rightRope.endPointTransform = this.endPointTransform;
            rightRope.endRigidbody = this.endRigidbody;
            rightRope.InitializeFromPositions(rightPos, rightVel);

            leftGO.transform.parent = transform.parent;
            rightGO.transform.parent = transform.parent;

            StartCoroutine(DisableBreakForFrames(leftRope, 3));
            StartCoroutine(DisableBreakForFrames(rightRope, 3));
        }
    }

    public void CutAtNode(int nodeIndex)
    {
        if (positions == null) return;
        int last = positions.Length - 1;
        if (nodeIndex <= 0 || nodeIndex >= last)
        {
            Debug.LogWarning("CutAt: invalid node index");
            return;
        }
        SplitAtSegment(nodeIndex - 1);
        if (singleUseBreak) Destroy(gameObject);
    }

    public void CutAtNearest(Vector2 worldPos)
    {
        if (positions == null) return;
        int nearest = -1;
        float best = float.MaxValue;
        for (int i = 0; i < positions.Length; i++)
        {
            float d = Vector2.SqrMagnitude(positions[i] - worldPos);
            if (d < best) { best = d; nearest = i; }
        }
        if (nearest <= 0) nearest = 1;
        if (nearest >= positions.Length - 1) nearest = positions.Length - 2;
        CutAtNode(nearest);
    }

    public void InitializeFromPositions(Vector2[] posArray, Vector2[] velArray = null)
    {
        nodeCount = Mathf.Max(2, posArray.Length);
        positions = new Vector2[nodeCount];
        oldPositions = new Vector2[nodeCount];
        lastIndex = nodeCount - 1;

        float len = 0f;
        for (int i = 0; i < nodeCount - 1; i++) len += Vector2.Distance(posArray[i], posArray[i + 1]);
        ropeLength = len;
        segmentLength = Mathf.Max(0.0001f, ropeLength / (nodeCount - 1));

        for (int i = 0; i < nodeCount; i++)
        {
            positions[i] = posArray[i];
            if (velArray != null && velArray.Length == posArray.Length)
            {
                oldPositions[i] = positions[i] - velArray[i] * Mathf.Max(lastFixedDt, 1e-6f);
            }
            else
            {
                oldPositions[i] = positions[i];
            }
        }

        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer != null) lineRenderer.positionCount = nodeCount;
    }

    void CopyLineRenderer(GameObject target)
    {
        LineRenderer src = this.lineRenderer;
        if (src == null) return;
        LineRenderer trg = target.AddComponent<LineRenderer>();
        trg.positionCount = src.positionCount;
        trg.widthCurve = src.widthCurve;
        trg.widthMultiplier = src.widthMultiplier;
        trg.material = src.material;
        trg.textureMode = src.textureMode;
        trg.numCapVertices = src.numCapVertices;
        trg.numCornerVertices = src.numCornerVertices;
        trg.useWorldSpace = true;
    }

    void CopyConfigTo(RopeVerletFull_Modified other)
    {
        if (other == null) return;
        other.nodeCount = this.nodeCount;
        other.ropeLength = this.ropeLength;
        other.constraintIterations = this.constraintIterations;
        other.maxSegmentStretch = this.maxSegmentStretch;
        other.damping = this.damping;
        other.gravity = this.gravity;
        other.maxNodeSpeed = this.maxNodeSpeed;
        other.bridgeK = this.bridgeK;
        other.bridgeDamping = this.bridgeDamping;
        other.bridgeStiffness = this.bridgeStiffness;
        other.maxBridgeForce = this.maxBridgeForce;
        other.autoBreakEnabled = this.autoBreakEnabled;
        other.breakDetectionEnabled = this.breakDetectionEnabled;
        other.breakThresholdFactor = this.breakThresholdFactor;
        other.singleUseBreak = this.singleUseBreak;
        other.midAnchorIsFree = this.midAnchorIsFree;
        other.midAnchorHasRigidbody = this.midAnchorHasRigidbody;
        other.midAnchorMass = this.midAnchorMass;
    }

    IEnumerator DisableBreakForFrames(RopeVerletFull_Modified rope, int frames)
    {
        if (rope == null) yield break;
        rope.breakDetectionEnabled = false;
        for (int i = 0; i < frames; i++) yield return new WaitForFixedUpdate();
        rope.breakDetectionEnabled = true;
    }

    void DrawRope()
    {
        if (lineRenderer == null) return;
        lineRenderer.positionCount = positions != null ? positions.Length : 0;
        for (int i = 0; i < positions.Length; i++) lineRenderer.SetPosition(i, positions[i]);
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmosNodes || positions == null) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < positions.Length; i++) Gizmos.DrawSphere((Vector3)positions[i], nodeGizmoSize);
    }
}