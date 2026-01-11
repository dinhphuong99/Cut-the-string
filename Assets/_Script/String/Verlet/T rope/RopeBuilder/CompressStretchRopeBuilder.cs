using System.Collections.Generic;
using UnityEngine;

public sealed class CompressStretchRopeBuilder : IRopeBuilder
{
    private readonly int fixedNodeCount;

    public CompressStretchRopeBuilder(int fixedNodeCount = 35)
    {
        this.fixedNodeCount = Mathf.Max(2, fixedNodeCount);
    }

    public List<RopeNode4> Build(RopeProfile profile, Vector3 start, Vector3 end, bool pinStart = true, bool pinEnd = true)
    {
        if (profile == null || profile.physics == null)
        {
            Debug.LogError("[CompressStretchRopeBuilder] Missing profile or profile.physics");
            return null;
        }

        float segmentLength = profile.physics.segmentLength;
        var nodes = new List<RopeNode4>(fixedNodeCount);

        float idealTotalLength = segmentLength * (fixedNodeCount - 1);
        float actualDist = Vector3.Distance(start, end);
        float scale = (idealTotalLength <= Mathf.Epsilon) ? 1f : (actualDist / idealTotalLength);

        Vector3 dir = (actualDist > Mathf.Epsilon) ? (end - start).normalized : Vector3.right;

        for (int i = 0; i < fixedNodeCount; i++)
        {
            float idealOffset = segmentLength * i;
            Vector3 pos = start + dir * (idealOffset * scale);

            var node = new RopeNode4(pos, false);
            node.oldPosition = pos;
            nodes.Add(node);
        }

        if (pinStart)
        {
            var n0 = nodes[0];
            n0.isPinned = true;
            n0.position = start;
            n0.oldPosition = start;
            nodes[0] = n0;
        }

        if (pinEnd)
        {
            var nl = nodes[nodes.Count - 1];
            nl.isPinned = true;
            nl.position = end;
            nl.oldPosition = end;
            nodes[nodes.Count - 1] = nl;
        }

        return nodes;
    }
}