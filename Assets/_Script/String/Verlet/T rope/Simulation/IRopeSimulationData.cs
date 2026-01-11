using System.Collections.Generic;
using UnityEngine;

public interface IRopeSimulationData
{
    IList<RopeNode4> Nodes { get; }

    float Gravity { get; }
    float Damping { get; }
    float SegmentLength { get; }
    int ConstraintIterations { get; }
    bool Simulate { get; }
    bool IsReady { get; }
    bool IsTaut { get; }
    float CurentLength { get; }
    float IdealLength { get; }
}