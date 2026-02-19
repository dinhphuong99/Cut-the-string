using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public sealed class RopeRetractController : MonoBehaviour
{
    [SerializeField] private MonoBehaviour ropeSource; // MUST implement IRopeRetractRelease
    private IRopeRetractRelease rope;

    [Header("Speed")]
    [SerializeField] private float retractSpeed = 5f;
    [SerializeField] private float releaseSpeed = 5f;

    private bool isRetracting;
    private bool isReleasing;

    private float retractProgress;
    private float releaseProgress;

    private void Awake()
    {
        if (ropeSource == null)
            ropeSource = GetComponent<MonoBehaviour>();

        rope = ropeSource as IRopeRetractRelease;

        if (rope == null)
        {
            Debug.LogError(
                $"[{name}] RopeRetractController requires a component implementing IRopeRetractRelease",
                this
            );
            enabled = false;
        }
    }

    public void StartRetract()
    {
        if (!IsValid()) return;
        isReleasing = false;
        isRetracting = true;
    }

    public void StopRetract() => isRetracting = false;

    public void StartRelease()
    {
        if (!IsValid()) return;
        isRetracting = false;
        isReleasing = true;
    }

    public void StopRelease() => isReleasing = false;

    private void FixedUpdate()
    {
        if (!IsValid()) return;

        float dt = Time.fixedDeltaTime;

        if (isRetracting)
            ApplyRetract(dt);

        if (isReleasing)
            ApplyRelease(dt);
    }

    private bool IsValid()
    {
        return rope != null && rope.IsReady && rope.Nodes != null && rope.Nodes.Count >= 2;
    }

    private void ApplyRetract(float delta)
    {
        IList<RopeNode4> nodes = rope.Nodes;

        int index = FindLastPinnedCloseToAnchor(nodes, 0.01f);
        int retractIndex = index + 1;

        if (retractIndex <= 0 || retractIndex >= nodes.Count)
        {
            isRetracting = false;
            return;
        }

        RopeNode4 n = nodes[retractIndex];
        RopeNode4 prev = nodes[retractIndex - 1];

        Vector2 nextPos =
            (retractIndex == nodes.Count - 1)
                ? n.position + Vector2.down
                : nodes[retractIndex + 1].position;

        retractProgress =
            Vector2.Distance(n.position, prev.position) - retractSpeed * delta;

        n.isPinned = true;
        n.position = GeometryUtils.GetPointOnLine(
            prev.position,
            nextPos,
            Mathf.Max(0f, retractProgress)
        );
        n.oldPosition = n.position;

        nodes[retractIndex] = n;
    }

    private void ApplyRelease(float delta)
    {
        IList<RopeNode4> nodes = rope.Nodes;

        int index = FindLastPinnedCloseToAnchor(nodes, 0.01f);
        if (index <= 0 || index >= nodes.Count)
        {
            isReleasing = false;
            return;
        }

        int releaseIndex =
            (index < nodes.Count - 1 && nodes[index + 1].isPinned)
                ? index + 1
                : index;

        if (releaseIndex <= 0 || releaseIndex >= nodes.Count)
        {
            isReleasing = false;
            return;
        }

        RopeNode4 n = nodes[releaseIndex];
        RopeNode4 prev = nodes[releaseIndex - 1];

        Vector2 nextPos =
            (releaseIndex == nodes.Count - 1)
                ? n.position + Vector2.down
                : nodes[releaseIndex + 1].position;

        releaseProgress =
            Vector2.Distance(n.position, prev.position) + releaseSpeed * delta;

        if (releaseProgress >= rope.SegmentLength)
        {
            n.position = GeometryUtils.GetPointOnLine(
                prev.position,
                nextPos,
                rope.SegmentLength
            );
            n.isPinned = false;
        }
        else
        {
            n.position = GeometryUtils.GetPointOnLine(
                prev.position,
                nextPos,
                releaseProgress
            );
        }

        if (releaseIndex == nodes.Count - 1)
            n.isPinned = true;

        n.oldPosition = n.position;
        nodes[releaseIndex] = n;
    }

    private int FindLastPinnedCloseToAnchor(IList<RopeNode4> nodes, float threshold)
    {
        Vector2 anchor = nodes[0].position;

        for (int i = nodes.Count - 2; i >= 0; i--)
        {
            if (!nodes[i].isPinned)
                continue;

            if (Vector2.Distance(nodes[i].position, anchor) <= threshold)
                return i;
        }
        return -1;
    }
}