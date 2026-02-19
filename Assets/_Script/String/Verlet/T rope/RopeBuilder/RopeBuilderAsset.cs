using UnityEngine;

public abstract class RopeBuilderAsset : ScriptableObject
{
    public abstract IRopeBuilder CreateBuilder(int nodeCount);
}
