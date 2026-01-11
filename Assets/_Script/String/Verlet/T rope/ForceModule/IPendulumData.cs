using UnityEngine;

public interface IPendulumData
{
    bool IsReady { get; }

    Transform Anchor { get; }

    Rigidbody2D Bob { get; }

    float IdealLength { get; }

    int Substeps { get; }

    PendulumPhysicsProfile PendulumPhysics { get; }
}
