using System.Collections.Generic;
using UnityEngine;

public class RopeRetractReleaseController : MonoBehaviour, IRopeModule
{
    [SerializeField] private KeyCode retractKey = KeyCode.W;
    [SerializeField] private KeyCode releaseKey = KeyCode.S;
    [SerializeField] private float baseRetractSpeed = 1.5f;
    [SerializeField] private float baseReleaseSpeed = 1.5f;
    [SerializeField] private float minSpeed = 0.2f;
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private KeyCode speedModifierKey = KeyCode.LeftShift;

    private IRopeRetractRelease rope;
    private bool initialized = false;
    private bool isRetracting = false;
    private bool isReleasing = false;
    private float speedMultiplier = 1f;

    public bool IsRetracting => isRetracting;
    public bool IsReleasing => isReleasing;

    public void Initialize(IRopeDataProvider provider)
    {
        if (initialized) return;
        if (provider is IRopeRetractRelease rr)
        {
            rope = rr;
            initialized = true;
        }
        else
        {
            Debug.LogWarning("[RopeRetractReleaseController] provider missing IRopeRetractRelease. Disabling.", this);
            enabled = false;
        }
    }

    //private void Update()
    //{
    //    float speedMul = Input.GetKey(speedModifierKey) ? 2f : 1f;
    //    baseRetractSpeed = Mathf.Clamp(baseRetractSpeed * speedMul, minSpeed, maxSpeed);
    //    baseReleaseSpeed = Mathf.Clamp(baseReleaseSpeed * speedMul, minSpeed, maxSpeed);

    //    isRetracting = Input.GetKey(retractKey);
    //    isReleasing = Input.GetKey(releaseKey);
    //}

    //private void FixedUpdate()
    //{
    //    if (!initialized || !rope.IsReady) return;
    //    if (isRetracting) ApplyRetract(Time.fixedDeltaTime);
    //    else if (isReleasing) ApplyRelease(Time.fixedDeltaTime);
    //}

    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = Mathf.Clamp(multiplier, 0.1f, 5f);
    }

    public void StartRetract()
    {
        isRetracting = true;
        isReleasing = false;
    }

    public void StopRetract()
    {
        isRetracting = false;
    }

    public void StartRelease()
    {
        isReleasing = true;
        isRetracting = false;
    }

    public void StopRelease()
    {
        isReleasing = false;
    }

    private void FixedUpdate()
    {
        if (isRetracting)
        {
            float speed = Mathf.Clamp(
                baseRetractSpeed * speedMultiplier,
                minSpeed,
                maxSpeed
            );

            ApplyRetract(speed * Time.fixedDeltaTime);
        }
        else if (isReleasing)
        {
            float speed = Mathf.Clamp(
                baseReleaseSpeed * speedMultiplier,
                minSpeed,
                maxSpeed
            );

            ApplyRelease(speed * Time.fixedDeltaTime);
        }
    }

    // simplified mechanics: operate on pinned nodes
    private void ApplyRetract(float dt)
    {
        var nodes = rope.Nodes;
        int lastPinned = FindLastPinnedCloseToAnchor(0.01f);
        int retractIndex = ResolveRetractIndex(lastPinned);
        if (retractIndex < 1) return;

        var n = nodes[retractIndex];
        var prev = nodes[retractIndex - 1];
        Vector2 next = retractIndex < nodes.Count - 1 ? nodes[retractIndex + 1].position : n.position + Vector2.down;
        float retractAmount = baseRetractSpeed * dt;
        float currentDist = Vector2.Distance(n.position, prev.position);
        float target = Mathf.Max(0f, currentDist - retractAmount);
        n.position = GeometryUtils.GetPointOnLine(prev.position, next, target);
        n.isPinned = true;
        n.oldPosition = n.position;
        nodes[retractIndex] = n;
    }

    private void ApplyRelease(float dt)
    {
        var nodes = rope.Nodes;
        int lastPinned = FindLastPinnedCloseToAnchor(0.01f);
        int releaseIndex = ResolveReleaseIndex(lastPinned);
        if (releaseIndex <= 0) return;

        var n = nodes[releaseIndex];
        var prev = nodes[releaseIndex - 1];
        Vector2 next = releaseIndex < nodes.Count - 1 ? nodes[releaseIndex + 1].position : n.position + Vector2.down;
        float releaseAmount = baseReleaseSpeed * dt;
        float current = Vector2.Distance(n.position, prev.position);
        float target = Mathf.Min(rope.SegmentLength, current + releaseAmount);
        n.position = GeometryUtils.GetPointOnLine(prev.position, next, target);
        if (target >= rope.SegmentLength) n.isPinned = false;
        n.oldPosition = n.position;
        nodes[releaseIndex] = n;
    }

    private int FindLastPinnedCloseToAnchor(float threshold)
    {
        var nodes = rope.Nodes;
        if (nodes.Count == 0) return -1;
        Vector2 anchor = nodes[0].position;
        for (int i = nodes.Count - 2; i >= 0; i--)
        {
            if (!nodes[i].isPinned) continue;
            if (Vector2.Distance(nodes[i].position, anchor) <= threshold) return i;
        }
        return -1;
    }

    private int ResolveRetractIndex(int index)
    {
        if (index < 0 || index >= rope.Nodes.Count - 1) return -1;
        return index + 1;
    }

    private int ResolveReleaseIndex(int index)
    {
        if (index <= 0 || index >= rope.Nodes.Count) return -1;
        if (index == rope.Nodes.Count - 1) return index;
        return rope.Nodes[index + 1].isPinned ? index + 1 : index;
    }
}