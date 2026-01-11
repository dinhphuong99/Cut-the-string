using UnityEngine;

public class RopeVerletVisual : MonoBehaviour
{
    [System.Serializable]
    public struct Node
    {
        public Vector2 position;
        public Vector2 oldPosition;
        public bool isPinned;
        public Node(Vector2 pos)
        {
            position = pos;
            oldPosition = pos;
            isPinned = false;
        }
    }
    public Transform startPoint; 
    public Transform endPoint;

    public Node[] nodes;
    public int nodeCount;
    public float gravity = 9.81f;
    public int constraintIterations = 6;
    [Range(0.9f, 0.999f)] public float damping = 0.995f;

    [Header("Rope Settings")]
    public float segmentLength = 0.2f;
    public float slackFactor = 0.76f;
    [SerializeField, Range(0f, 1f)]
    private float sagAmount = 0.5f;

    // Thay vì tự có LineRenderer, ta nhận từ ngoài
    private LineRenderer line;

    [SerializeField] LineRenderer externalLine;

    public void SetLineRenderer(LineRenderer externalLine)
    {
        line = externalLine;
    }

    private void Start()
    {
        SetLineRenderer(externalLine);
        GenerateRope(startPoint, endPoint);
    }

    public void GenerateRope(Transform a, Transform b)
    {
        if (a == null || b == null)
        {
            Debug.LogWarning("Missing rope endpoints.");
            return;
        }

        Vector2 start = a.position;
        Vector2 end = b.position;

        float straightDist = Vector2.Distance(start, end);
        float ropeLength = straightDist * slackFactor;
        nodeCount = Mathf.Max(2, Mathf.CeilToInt(ropeLength / segmentLength));

        nodes = new Node[nodeCount];
        for (int i = 0; i < nodeCount; i++)
        {
            float t = i / (float)(nodeCount - 1);
            Vector2 p = Vector2.Lerp(start, end, t);

            // tính độ võng ban đầu
            float sag = Mathf.Sin(t * Mathf.PI) * straightDist * sagAmount;
            p.y -= sag;

            nodes[i] = new Node(p);
        }

        nodes[0].isPinned = true;
        nodes[^1].isPinned = true;

        if (line != null)
        {
            line.positionCount = nodeCount;
            UpdateLine();
        }
    }

    public void Simulate(float deltaTime)
    {
        if (nodes == null || nodes.Length == 0) return;

        Vector2 gravityVec = new Vector2(0, -gravity);
        for (int i = 0; i < nodeCount; i++)
        {
            if (nodes[i].isPinned) continue;
            Vector2 pos = nodes[i].position;
            Vector2 oldPos = nodes[i].oldPosition;
            Vector2 vel = (pos - oldPos) * damping;
            nodes[i].oldPosition = pos;
            nodes[i].position = pos + vel + gravityVec * deltaTime * deltaTime;
        }

        for (int k = 0; k < constraintIterations; k++)
        {
            for (int i = 0; i < nodeCount - 1; i++)
            {
                Node nA = nodes[i];
                Node nB = nodes[i + 1];

                Vector2 delta = nB.position - nA.position;
                float dist = delta.magnitude;
                float diff = (dist - segmentLength) / dist;
                Vector2 correction = delta * 0.5f * diff;

                if (!nA.isPinned)
                    nA.position += correction;
                if (!nB.isPinned)
                    nB.position -= correction;

                nodes[i] = nA;
                nodes[i + 1] = nB;
            }
        }

        UpdateLine();
    }

    public void UpdateLine()
    {
        if (line == null) return;
        for (int i = 0; i < nodeCount; i++)
            line.SetPosition(i, nodes[i].position);
    }
}