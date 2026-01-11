using UnityEngine;

public sealed class FreezeConstraint : IRopeConstraint
{
    private bool active = true;

    public bool IsActive => active;

    public void Release()
    {
        active = false;
    }

    public void Apply(ref RopeNode4 node)
    {
        node.oldPosition = node.position;
    }
}