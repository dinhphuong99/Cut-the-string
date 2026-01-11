using System.Collections.Generic;

public static class RopeAnchorRegistry
{
    private static readonly HashSet<IRopeAnchor> _anchors = new();

    public static IReadOnlyCollection<IRopeAnchor> Anchors => _anchors;

    public static void Register(IRopeAnchor anchor)
    {
        _anchors.Add(anchor);
    }

    public static void Unregister(IRopeAnchor anchor)
    {
        _anchors.Remove(anchor);
    }
}