using UnityEngine;

[CreateAssetMenu(fileName = "RopePhysicsProfile", menuName = "Physics Profiles/Rope Physics Profile")]
public class RopePhysicsProfile : ScriptableObject
{
    [Header("Global Forces")]
    public float gravity = 9.81f;
    [Range(0.8f, 0.9999f)] public float damping = 0.9999f;
    public int substeps = 10;

    [Header("Physical Damping (Realistic)")]
    [Tooltip("Damping ratio. 0.2–0.6 hợp lý nhất cho pendulum, 1.0 = critically damped.")]
    [Range(0f, 2f)] public float dampingZeta = 0.3f;

    [Tooltip("Nếu không dùng auto, sẽ dùng c này: F = -c * v.")]
    public float dampingC_Fixed = 2f;

    [Header("Rope Parameters")]
    public float segmentLength = 0.2f;
    public float slackFactor = 0.76f;
    [Range(0f, 1f)] public float sagAmount = 0.5f;
    public int constraintIterations = 6;

    [Header("Spring Parameters")]
    public float k = 50f;
    public float maxStretchFactor = 1.3f;

    [Tooltip("Nếu bật, dây hoạt động như lò xo (đẩy khi nén, kéo khi giãn). Nếu tắt, chỉ kéo khi giãn.")]
    public bool bidirectionalElasticity = false;

    [Header("Stability")]
    [Tooltip("Giới hạn vận tốc cực đại để tránh rung mạnh hoặc explosion.")]
    public float maxVelocity = 200f;

}