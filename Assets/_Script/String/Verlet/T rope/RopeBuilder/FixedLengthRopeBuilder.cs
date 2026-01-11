using System.Collections.Generic;
using UnityEngine;

public sealed class FixedLengthRopeBuilder : IRopeBuilder
{
    private readonly int nodeCount;

    public FixedLengthRopeBuilder(int nodeCount = 20)
    {
        this.nodeCount = Mathf.Max(2, nodeCount);
    }

    public List<RopeNode4> Build(RopeProfile profile, Vector3 start, Vector3 end, bool pinStart = true, bool pinEnd = true)
    {
        if (profile == null || profile.physics == null)
        {
            Debug.LogError("[FixedLengthRopeBuilder] Missing profile or profile.physics");
            return null;
        }

        float segmentLength = profile.physics.segmentLength;
        var nodes = new List<RopeNode4>(nodeCount);
        Vector3 dir = (end - start).normalized;

        for (int i = 0; i < nodeCount; i++)
        {
            Vector3 pos = start + dir * (segmentLength * i);
            var n = new RopeNode4(pos, false) { oldPosition = pos };
            nodes.Add(n);
        }

        if (pinStart)
            nodes[0] = new RopeNode4(start, true) { oldPosition = start };

        if (pinEnd)
            nodes[^1] = new RopeNode4(end, true) { oldPosition = end };

        return nodes;
    }
}