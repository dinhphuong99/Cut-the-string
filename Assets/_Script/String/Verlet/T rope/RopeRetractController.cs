using UnityEngine;

[DisallowMultipleComponent]
public class RopeRetractController : MonoBehaviour
{
    [SerializeField] private VerletRope7 rope;

    [Header("Speed")]
    [SerializeField] private float retractSpeed = 5f;
    [SerializeField] private float releaseSpeed = 5f;

    private bool isRetracting;
    private bool isReleasing;

    private float retractProgress;
    private float releaseProgress;

    private int retractIndex;
    private int releaseIndex;

    void Awake()
    {
        if (!rope)
            rope = GetComponent<VerletRope7>();
    }

    public void StartRetract()
    {
        isReleasing = false;
        isRetracting = true;
    }

    public void StopRetract()
    {
        isRetracting = false;
    }

    public void StartRelease()
    {
        isRetracting = false;
        isReleasing = true;
    }

    public void StopRelease()
    {
        isReleasing = false;
    }

    void FixedUpdate()
    {
        if (!rope || !rope.simulate) return;

        float dt = Time.fixedDeltaTime;

        if (isRetracting)
            ApplyRetract(dt);

        if (isReleasing)
            ApplyRelease(dt);
    }

    void ApplyRetract(float delta)
    {
        int index = FindLastPinnedCloseToAnchor(0.01f);
        retractIndex = ResolveRetractIndex(index);

        if (retractIndex < 1)
        {
            isRetracting = false;
            return;
        }

        var nodes = rope.nodes;

        RopeNode4 n = nodes[retractIndex];
        RopeNode4 prev = nodes[retractIndex - 1];

        Vector2 nextPos = (retractIndex == rope.nodeCount - 1)
            ? n.position + Vector2.down
            : nodes[retractIndex + 1].position;

        retractProgress = Vector2.Distance(n.position, prev.position) - retractSpeed * delta;

        n.isPinned = true;
        n.position = GeometryUtils.GetPointOnLine(
            prev.position,
            nextPos,
            Mathf.Max(0f, retractProgress)
        );

        n.oldPosition = n.position;
        nodes[retractIndex] = n;
    }

    void ApplyRelease(float delta)
    {
        int index = FindLastPinnedCloseToAnchor(0.01f);
        releaseIndex = ResolveReleaseIndex(index);

        if (releaseIndex <= 0)
        {
            isReleasing = false;
            return;
        }

        var nodes = rope.nodes;

        RopeNode4 n = nodes[releaseIndex];
        RopeNode4 prev = nodes[releaseIndex - 1];

        Vector2 nextPos = (releaseIndex == rope.nodeCount - 1)
            ? n.position + Vector2.down
            : nodes[releaseIndex + 1].position;

        releaseProgress =
            Vector2.Distance(n.position, prev.position) + releaseSpeed * delta;

        if (releaseProgress >= rope.segmentLength)
        {
            n.position = GeometryUtils.GetPointOnLine(
                prev.position, nextPos, rope.segmentLength
            );
            n.isPinned = false;
        }
        else
        {
            n.position = GeometryUtils.GetPointOnLine(
                prev.position, nextPos, releaseProgress
            );
        }

        if (releaseIndex == rope.nodeCount - 1)
            n.isPinned = true;

        n.oldPosition = n.position;
        nodes[releaseIndex] = n;
    }

    int FindLastPinnedCloseToAnchor(float threshold)
    {
        Vector2 anchorPos = rope.nodes[0].position;

        for (int i = rope.nodes.Count - 2; i >= 0; i--)
        {
            if (!rope.nodes[i].isPinned)
                continue;

            if (Vector2.Distance(rope.nodes[i].position, anchorPos) <= threshold)
                return i;
        }

        return -1;
    }

    int ResolveReleaseIndex(int index)
    {
        if (index <= 0 || index >= rope.nodes.Count)
            return -1;

        if (index == rope.nodes.Count - 1)
            return index;

        return rope.nodes[index + 1].isPinned ? index + 1 : index;
    }

    int ResolveRetractIndex(int index)
    {
        if (index < 0 || index >= rope.nodes.Count - 1)
            return -1;

        return index + 1;
    }
}