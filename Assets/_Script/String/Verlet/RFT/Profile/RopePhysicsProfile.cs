using UnityEngine;

[CreateAssetMenu(fileName = "RopePhysicsProfile", menuName = "Physics Profiles/Rope Physics Profile")]
public class RopePhysicsProfile : ScriptableObject
{
    [Header("Global Forces")]
    public float gravity = 9.81f;
    [Range(0.8f, 0.9999f)] public float damping = 0.995f;
    [Min(1)] public int substeps = 1;

    [Header("Rope Parameters")]
    public float segmentLength = 0.2f;
    public float slackFactor = 1.0f;
    [Min(1)] public int constraintIterations = 6;

    [Header("Elasticity / Spring (used by modules if needed)")]
    public float k = 50f;
    [Tooltip("Allow max stretch of whole rope relative to ideal length")]
    public float maxStretchFactor = 1.3f;
    public bool bidirectionalElasticity = false;

    [Header("Stability")]
    public float maxVelocity = 200f;
}