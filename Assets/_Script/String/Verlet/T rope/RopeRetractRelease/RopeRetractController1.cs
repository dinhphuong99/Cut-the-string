using UnityEngine;

[DisallowMultipleComponent]
public class RopeRetractReleaseController1 : MonoBehaviour, IRopeModule
{
    private IRopeRetractRelease rope;
    private bool initialized = false;

    [SerializeField] private float baseRetractSpeed = 1.5f;
    [SerializeField] private float baseReleaseSpeed = 1.5f;

    [SerializeField] private float minSpeed = 0.5f;
    [SerializeField] private float maxSpeed = 10f;

    [SerializeField] private float retractSpeed = 1.5f;

    [SerializeField] private float releaseSpeed = 1.5f;

    [SerializeField] private KeyCode retractKey = KeyCode.W;

    [SerializeField] private KeyCode releaseKey = KeyCode.S;

    [SerializeField] private KeyCode addSpeedKey = KeyCode.LeftShift;

    int index = 0;

    private int retractIndex = 1;

    [SerializeField] private int releaseIndex = 1;

    private bool isRetracting = false;

    private bool isReleasing = false;

    private float retractProgress = 0f;

    private float releaseProgress = 0f;

    public void Initialize(IRopeDataProvider rp)
    {
        if (initialized) return;

        rope = rp as IRopeRetractRelease;
        if (rope == null)
        {
            Debug.LogWarning("[RopeRetractRelease] IRopeRetractRelease not supported", this);
            enabled = false;
            return;
        }

        initialized = true;
    }

    void Update()
    {
        float speedModifier = Input.GetKey(addSpeedKey) ? 2f : 1f;

        retractSpeed = Mathf.Clamp(
            baseRetractSpeed * speedModifier,
            minSpeed,
            maxSpeed
        );

        releaseSpeed = Mathf.Clamp(
            baseReleaseSpeed * speedModifier,
            minSpeed,
            maxSpeed
        );

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

    }

    void FixedUpdate()
    {
        if (!initialized || !rope.IsReady)
            return;

        if (isRetracting)
            ApplyRetract(Time.fixedDeltaTime);
        else if (isReleasing)
            ApplyRelease(Time.fixedDeltaTime);
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
        if (!initialized || !rope.IsReady) return;

        if (!isRetracting) return;

        index = FindLastPinnedCloseToAnchor(0.01f);

        retractIndex = ResolveRetractIndex(index);

        if (retractIndex < 1)

        {

            isRetracting = false;

            return;

        }

        RopeNode4 n = rope.Nodes[retractIndex];

        RopeNode4 prev = rope.Nodes[retractIndex - 1];

        Vector2 nextPos;

        if (retractIndex == rope.Nodes.Count - 1)

        {

            nextPos = rope.Nodes[releaseIndex].position + Vector2.down;

        }

        else

        {

            nextPos = rope.Nodes[retractIndex + 1].position;

        }

        retractProgress = Vector2.Distance(n.position, prev.position) - retractSpeed * delta;

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

        rope.Nodes[retractIndex] = n;

    }

    private void ApplyRelease(float delta)

    {
        if (!initialized || !rope.IsReady) return;
        if (!isReleasing) return;

        index = FindLastPinnedCloseToAnchor(0.01f);

        releaseIndex = ResolveReleaseIndex(index);

        if (releaseIndex <= 0)

        {

            isReleasing = false;

            return;

        }

        RopeNode4 n = rope.Nodes[releaseIndex];

        RopeNode4 prev = rope.Nodes[releaseIndex - 1];

        Vector2 nextPos;

        if (releaseIndex == rope.Nodes.Count - 1)
        {
            nextPos = rope.Nodes[releaseIndex].position + Vector2.down;
        }
        else
        {
            nextPos = rope.Nodes[releaseIndex + 1].position;
        }

        releaseProgress = Vector2.Distance(n.position, prev.position) + releaseSpeed * delta;

        if (releaseProgress >= rope.SegmentLength)

        {

            n.position = GeometryUtils.GetPointOnLine(prev.position, nextPos, rope.SegmentLength);

            n.isPinned = false;

        }

        else

        {

            n.position = GeometryUtils.GetPointOnLine(prev.position, nextPos, releaseProgress);

        }

        if (releaseIndex == rope.Nodes.Count - 1)

        {

            n.isPinned = true;

        }

        n.oldPosition = n.position;

        rope.Nodes[releaseIndex] = n;

    }

    int FindLastPinnedCloseToAnchor(float threshold)

    {

        Vector2 anchorPos = rope.Nodes[0].position;

        for (int i = rope.Nodes.Count - 2; i >= 0; i--)

        {

            if (!rope.Nodes[i].isPinned)

                continue;

            float dist = Vector2.Distance(rope.Nodes[i].position, anchorPos);

            if (dist <= threshold)

                return i;

        }

        return -1;

    }

    int ResolveReleaseIndex(int index)

    {

        if (index <= 0 || index > rope.Nodes.Count - 1)

            return -1;

        if (index == rope.Nodes.Count - 1)

            return index;

        if (rope.Nodes[index + 1].isPinned)

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

        if (index < 0 || index >= rope.Nodes.Count - 1)

            return -1;

        return index + 1;

    }
}
