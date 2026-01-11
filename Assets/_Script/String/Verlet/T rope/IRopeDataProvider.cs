using System;
using System.Collections.Generic;
using UnityEngine;

public interface IRopeDataProvider
{
    int NodeCount { get; }
    IReadOnlyList<Vector2> Nodes { get; }
    event Action OnNodesReady;
    bool ShouldBlink { get; }

    bool IsReady { get; }
}
