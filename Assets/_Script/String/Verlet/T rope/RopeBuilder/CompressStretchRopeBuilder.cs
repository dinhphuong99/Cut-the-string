using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Default implementation of IRopeBuilder used by RopeBuilder.
/// Produces evenly spaced nodes between start and end and respects the profile.segmentLength.
/// </summary>
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

        float segLen = profile.physics.segmentLength;
        var nodes = new List<RopeNode4>(fixedNodeCount);

        float idealTotal = segLen * (fixedNodeCount - 1);
        float actualDist = Vector3.Distance(start, end);
        float scale = (idealTotal <= Mathf.Epsilon) ? 1f : (actualDist / idealTotal);
        Vector3 dir = (actualDist > Mathf.Epsilon) ? (end - start).normalized : Vector3.right;

        for (int i = 0; i < fixedNodeCount; i++)
        {
            float idealOffset = segLen * i;
            Vector3 pos = start + dir * (idealOffset * scale);
            var node = new RopeNode4(pos, false);
            node.oldPosition = pos;
            nodes.Add(node);
        }

        if (pinStart && nodes.Count > 0)
        {
            var n0 = nodes[0];
            n0.isPinned = true;
            n0.position = start;
            n0.oldPosition = start;
            nodes[0] = n0;
        }

        if (pinEnd && nodes.Count > 1)
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
