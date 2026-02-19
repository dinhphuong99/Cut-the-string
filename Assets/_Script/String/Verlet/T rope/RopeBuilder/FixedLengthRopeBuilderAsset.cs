using UnityEngine;

[CreateAssetMenu(menuName = "Rope/Builder/Fixed Length")]
public sealed class FixedLengthRopeBuilderAsset : RopeBuilderAsset
{
    public override IRopeBuilder CreateBuilder(int nodeCount)
    {
        return new FixedLengthRopeBuilder(nodeCount);
    }
}
