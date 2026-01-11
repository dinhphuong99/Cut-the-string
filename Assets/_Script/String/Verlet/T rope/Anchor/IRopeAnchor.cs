using UnityEngine;
using System;

public interface IRopeAnchor
{
    Transform Transform { get; }

    bool IsActive { get; }

    event Action<IRopeAnchor> OnAnchorInvalidated;
}