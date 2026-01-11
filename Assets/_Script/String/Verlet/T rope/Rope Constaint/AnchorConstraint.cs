using UnityEngine;

public sealed class AnchorConstraint : IRopeConstraint
{
    private readonly Transform anchor;

    public AnchorConstraint(Transform anchor)
    {
        this.anchor = anchor;
    }

    public bool IsActive => anchor != null;

    public void Apply(ref RopeNode4 node)
    {
        if (!anchor) return;

        node.position = anchor.position;
        node.oldPosition = node.position;
    }
}
