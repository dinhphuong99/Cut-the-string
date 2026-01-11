using UnityEngine;

/// <summary>
/// Cấu hình vật lý chung cho tất cả node trong dây.
/// Dùng ScriptableObject để dễ tinh chỉnh và chia sẻ.
/// </summary>
[CreateAssetMenu(menuName = "Rope/Node Settings")]
public class RopeNodeSettings : ScriptableObject
{
    [Header("Mass & Damping")]
    [Tooltip("Khối lượng mỗi node.")]
    public float mass = 0.01f;

    [Tooltip("Hệ số cản, giảm dần vận tốc sau mỗi bước mô phỏng (0.9–1).")]
    [Range(0.9f, 1f)]
    public float damping = 0.98f;

    [Header("Elasticity")]
    [Tooltip("Độ cứng của dây giữa 2 node liền kề.")]
    public float stiffness = 80f; // k_s

    [Tooltip("Chiều dài nghỉ (rest length) giữa 2 node liền kề.")]
    public float restLength = 0.2f;

    [Tooltip("Độ cứng cho lực kéo (nếu dây bị căng nhiều).")]
    public float tensionStiffness = 500f;

    [Tooltip("Độ căng ban đầu (T0). Nếu = 0, không dùng pretension.")]
    public float pretension = 0f;

    [Header("Bending")]
    [Tooltip("Kích hoạt lực uốn giữa node i và i+2.")]
    public bool useBendI2 = false;

    [Tooltip("Độ cứng cho lực uốn (bending).")]
    public float bend2Stiffness = 20f;

    [Header("External Forces")]
    [Tooltip("Hướng và độ lớn của trọng lực.")]
    public Vector3 gravity = new Vector3(0, -9.81f, 0);

    [Tooltip("Tỷ lệ ảnh hưởng của trọng lực (0 = tắt gravity).")]
    public float gravityScale = 1f;

    [Header("Simulation")]
    [Range(1, 10)]
    [Tooltip("Số lần lặp constraint mỗi bước mô phỏng.")]
    public int constraintIterations = 6;
}