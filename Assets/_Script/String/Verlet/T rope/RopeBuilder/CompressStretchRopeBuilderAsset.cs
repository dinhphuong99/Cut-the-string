using UnityEngine;

[CreateAssetMenu(menuName = "Rope/Builder/Compress Stretch")]
public sealed class CompressStretchRopeBuilderAsset : RopeBuilderAsset
{
    public override IRopeBuilder CreateBuilder(int nodeCount)
    {
        return new CompressStretchRopeBuilder(nodeCount);
    }
}
