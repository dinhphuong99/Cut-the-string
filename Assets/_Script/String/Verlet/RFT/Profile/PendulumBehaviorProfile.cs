using UnityEngine;

[CreateAssetMenu(fileName = "PendulumBehaviorProfile", menuName = "Rope/Pendulum Behavior")]
public class PendulumBehaviorProfile : ScriptableObject
{
    [Header("Behavior")]
    [Tooltip("Multiplier applied to rope.physics.k when pendulum applies spring force.")]
    public float springKMultiplier = 1.0f;

    [Tooltip("Directional damping along rope (multiply dampingZeta style).")]
    public float dampingAlongMultiplier = 1.0f;

    [Tooltip("Small isotropic drag applied to bob Rigidbody2D.")]
    public float airDragC = 0.05f;

    public bool enabled = true;
}