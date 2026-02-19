using UnityEngine;

[CreateAssetMenu(fileName = "RopeProfile", menuName = "Rope/Rope Profile")]
public class RopeProfile : ScriptableObject
{
    public RopePhysicsProfile physics;
    public RopeRenderProfile render;
    public RopeBuilderAsset builder;
}
