using System.Collections.Generic;

public interface IRopeRetractRelease
{
    IList<RopeNode4> Nodes { get; }
    float SegmentLength { get; }
    bool IsReady { get; }
}