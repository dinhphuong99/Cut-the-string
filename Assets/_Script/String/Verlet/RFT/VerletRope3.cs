using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(LineRenderer))]
public class VerletRope3 : MonoBehaviour
{
    [System.Serializable]
    public struct RopeNode
    {
        public Vector2 position;
        public Vector2 oldPosition;
        public bool isPinned;

        public RopeNode(Vector2 pos, bool pinned = false)
        {
            position = pos;
            oldPosition = pos;
            isPinned = pinned;
        }

        public void SetPosition(Vector2 newPos)
        {
            oldPosition = position;
            position = newPos;
        }

        public void ResetOldPosition()
        {
            oldPosition = position;
        }
    }

    [Header("Rope Setup")]
    public Transform startPoint;
    public Transform endPoint;
    [Range(0.05f, 1f)] public float segmentLength = 0.2f;
    [Range(2, 100)] public int constraintIterations = 6;
    [Range(0.8f, 0.999f)] public float damping = 0.995f;
    public float gravity = 9.81f;
    public float recoilStrength = 1.5f;

    [Header("Debug Options")]
    public bool showGizmos = true;
    public bool showErrorNodes = true;

    private List<RopeNode> nodes = new List<RopeNode>();
    private LineRenderer lineRenderer;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    void Start()
    {
        try
        {
            InitializeRope();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[{name}] Initialization failed: {e.Message}\n{e.StackTrace}");
        }
    }

    void InitializeRope()
    {
        nodes.Clear();

        if (startPoint == null || endPoint == null)
        {
            Debug.LogError($"[{name}] Missing startPoint or endPoint reference.");
            return;
        }

        float distance = Vector2.Distance(startPoint.position, endPoint.position);
        int nodeCount = Mathf.Max(2, Mathf.CeilToInt(distance / segmentLength));
        Vector2 dir = (endPoint.position - startPoint.position).normalized;

        for (int i = 0; i < nodeCount; i++)
        {
            Vector2 pos = (Vector2)startPoint.position + dir * segmentLength * i;
            bool pinned = (i == 0);
            nodes.Add(new RopeNode(pos, pinned));
        }

        if (lineRenderer != null)
            lineRenderer.positionCount = nodeCount;

        Debug.Log($"[{name}] Rope initialized with {nodeCount} nodes.");
    }

    void Update()
    {
        try
        {
            Simulate();
            UpdateVisual();

            // Debug cut test
            if (Input.GetKeyDown(KeyCode.C))
                CutAtNode(nodes.Count / 2);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[{name}] Simulation failed: {e.Message}\n{e.StackTrace}");
        }
    }

    void Simulate()
    {
        if (nodes == null || nodes.Count < 2)
            return;

        for (int i = 0; i < nodes.Count; i++)
        {
            RopeNode n = nodes[i];
            if (n.isPinned)
            {
                if (startPoint != null)
                    n.position = startPoint.position;
                nodes[i] = n;
                continue;
            }

            Vector2 velocity = (n.position - n.oldPosition) * damping;
            n.oldPosition = n.position;
            n.position += velocity + Vector2.down * gravity * Time.deltaTime;

            if (float.IsNaN(n.position.x) || float.IsNaN(n.position.y))
            {
                Debug.LogError($"[{name}] Node {i} has NaN position!");
                n.position = Vector2.zero;
            }

            nodes[i] = n;
        }

        for (int iter = 0; iter < constraintIterations; iter++)
        {
            for (int i = 0; i < nodes.Count - 1; i++)
                ApplyConstraint(i);

            if (endPoint != null && nodes.Count > 1)
            {
                RopeNode last = nodes[nodes.Count - 1];
                last.position = endPoint.position;
                nodes[nodes.Count - 1] = last;
            }
        }
    }

    void ApplyConstraint(int index)
    {
        try
        {
            if (index < 0 || index >= nodes.Count - 1)
                return;

            RopeNode a = nodes[index];
            RopeNode b = nodes[index + 1];
            Vector2 diff = b.position - a.position;
            float dist = diff.magnitude;
            if (dist == 0) return;

            float error = dist - segmentLength;
            Vector2 correction = (diff / dist) * (error * 0.5f);

            if (!a.isPinned) a.position += correction;
            if (!b.isPinned) b.position -= correction;

            nodes[index] = a;
            nodes[index + 1] = b;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[{name}] ApplyConstraint error at index {index}: {e.Message}");
        }
    }

    void UpdateVisual()
    {
        if (lineRenderer == null || nodes == null) return;

        int count = nodes.Count;
        lineRenderer.positionCount = count;

        for (int i = 0; i < count; i++)
        {
            try
            {
                lineRenderer.SetPosition(i, nodes[i].position);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[{name}] LineRenderer failed at index {i}: {e.Message}");
            }
        }
    }

    // ---------------- CUT SYSTEM ---------------- //

    public void CutAtNode(int index)
    {
        if (nodes == null || nodes.Count < 3)
        {
            Debug.LogWarning($"[{name}] Rope too short to cut.");
            return;
        }
        if (index <= 0 || index >= nodes.Count - 1)
        {
            Debug.LogWarning($"[{name}] Invalid cut index: {index}");
            return;
        }

        Debug.Log($"[{name}] Cutting rope at node {index}/{nodes.Count}");

        List<RopeNode> leftNodes = SafeSubList(nodes, 0, index + 1);
        List<RopeNode> rightNodes = SafeSubList(nodes, index, nodes.Count - index);

        if (leftNodes == null || rightNodes == null)
        {
            Debug.LogError($"[{name}] Failed to split node list!");
            return;
        }

        Vector2 cutPos = nodes[index].position;
        leftNodes[leftNodes.Count - 1] = new RopeNode(cutPos);
        rightNodes[0] = new RopeNode(cutPos);

        Vector2 dir = (nodes[Mathf.Min(index + 1, nodes.Count - 1)].position - nodes[Mathf.Max(index - 1, 0)].position).normalized;
        ApplyRecoil(leftNodes, -dir * recoilStrength);
        ApplyRecoil(rightNodes, dir * recoilStrength);

        SpawnRopePiece(leftNodes, "_LeftPiece");
        SpawnRopePiece(rightNodes, "_RightPiece");

        Destroy(gameObject);
    }

    List<RopeNode> SafeSubList(List<RopeNode> source, int start, int count)
    {
        if (source == null) return null;
        if (start < 0 || start >= source.Count) return null;
        if (count <= 0 || start + count > source.Count) count = source.Count - start;

        List<RopeNode> sub = new List<RopeNode>();
        for (int i = start; i < start + count && i < source.Count; i++)
            sub.Add(source[i]);
        return sub;
    }

    void ApplyRecoil(List<RopeNode> list, Vector2 recoil)
    {
        if (list == null) return;
        for (int i = 0; i < list.Count; i++)
        {
            float falloff = (float)i / list.Count;
            RopeNode n = list[i];
            n.position += recoil * (1f - falloff) * 0.1f;
            list[i] = n;
        }
    }

    void SpawnRopePiece(List<RopeNode> nodeList, string nameSuffix)
    {
        try
        {
            if (nodeList == null || nodeList.Count < 2)
            {
                Debug.LogWarning($"[{name}] Cannot spawn {nameSuffix}: invalid node list.");
                return;
            }

            GameObject go = new GameObject(name + nameSuffix);
            var rope = go.AddComponent<VerletRope3>();
            var lr = go.AddComponent<LineRenderer>();

            CopyLineRendererSettings(lineRenderer, lr);

            rope.lineRenderer = lr;
            rope.segmentLength = segmentLength;
            rope.constraintIterations = constraintIterations;
            rope.damping = damping;
            rope.gravity = gravity;
            rope.recoilStrength = recoilStrength;

            rope.nodes = new List<RopeNode>(nodeList);
            rope.startPoint = null;
            rope.endPoint = null;

            lr.positionCount = nodeList.Count;
            rope.UpdateVisual();

            Debug.Log($"[{name}] Spawned new rope piece {nameSuffix} with {nodeList.Count} nodes.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[{name}] SpawnRopePiece failed ({nameSuffix}): {e.Message}");
        }
    }

    void CopyLineRendererSettings(LineRenderer src, LineRenderer dst)
    {
        if (src == null || dst == null) return;
        if (src.material != null)
            dst.material = new Material(src.material);
        dst.widthMultiplier = src.widthMultiplier;
        dst.colorGradient = src.colorGradient;
        dst.textureMode = src.textureMode;
        dst.numCornerVertices = src.numCornerVertices;
        dst.numCapVertices = src.numCapVertices;
        dst.alignment = src.alignment;
        dst.shadowCastingMode = src.shadowCastingMode;
        dst.receiveShadows = src.receiveShadows;
        dst.sortingLayerID = src.sortingLayerID;
        dst.sortingOrder = src.sortingOrder;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!showGizmos || nodes == null || nodes.Count == 0) return;

        for (int i = 0; i < nodes.Count; i++)
        {
            RopeNode n = nodes[i];
            Gizmos.color = n.isPinned ? Color.green : Color.yellow;
            Gizmos.DrawSphere(n.position, 0.02f);

            if (showErrorNodes)
            {
                if (float.IsNaN(n.position.x) || float.IsNaN(n.position.y))
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(n.position, 0.04f);
                    Handles.Label(n.position, $"NaN Node {i}");
                }
            }
        }
    }
#endif
}