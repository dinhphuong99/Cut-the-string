using System;
using System.Collections.Generic;
using UnityEngine;

public interface IRopeDataProvider
{
    bool IsReady { get; }
    int NodeCount { get; }
    IReadOnlyList<Vector2> NodesPositions { get; }
    bool ShouldBlink { get; }
    event Action OnNodesReady;
}