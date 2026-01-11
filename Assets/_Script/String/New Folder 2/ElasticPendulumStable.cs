using UnityEngine;

/// <summary>
/// Mô phỏng con lắc đàn hồi có hiệu chỉnh vị trí (constraint correction).
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class ElasticPendulumStable : MonoBehaviour
{
    public Transform mount;
    public Rigidbody2D bob;
    public float restLength = 3f;     // Chiều dài nghỉ của dây
    public float stiffness = 50f;     // Độ cứng dây
    public float gravity = 9.81f;
    public float damping = 0.995f;    // Giảm dao động nhẹ
    public bool drawLine = true;
    public float correctionFactor = 0.5f; // Hệ số chỉnh vị trí (0-1)

    private LineRenderer line;

    void Start()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 2;
        bob.gravityScale = 0f;
    }

    void FixedUpdate()
    {
        Vector2 pivot = mount.position;
        Vector2 pos = bob.position;
        Vector2 dir = pos - pivot;
        float dist = dir.magnitude;
        if (dist < 0.0001f) return;

        Vector2 dirNorm = dir / dist;

        // --- Áp dụng lực ---
        Vector2 force = Vector2.down * gravity * bob.mass;

        // Lực đàn hồi theo Hooke
        float stretch = dist - restLength;
        force += -dirNorm * (stretch * stiffness);

        // Áp dụng lực tổng
        bob.AddForce(force);

        // Giảm năng lượng thừa
        bob.linearVelocity *= damping;

        // --- Constraint Correction ---
        // Sau khi cập nhật vận tốc, sửa lại vị trí để tránh sai số tích lũy
        pos = bob.position;
        dir = pos - pivot;
        dist = dir.magnitude;

        if (dist > 0.0001f)
        {
            float error = dist - restLength;
            Vector2 correction = dir.normalized * error * correctionFactor;
            bob.position -= correction; // Kéo bob về gần đúng độ dài nghỉ
        }

        // --- Vẽ dây ---
        if (drawLine)
        {
            line.SetPosition(0, pivot);
            line.SetPosition(1, bob.position);
        }
    }
}