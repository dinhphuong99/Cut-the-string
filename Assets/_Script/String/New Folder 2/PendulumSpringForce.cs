using UnityEngine;

/// <summary>
/// Mô phỏng con lắc có dây đàn hồi (spring pendulum).
/// Dây co giãn theo định luật Hooke và có giảm chấn.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class PendulumSpringForce : MonoBehaviour
{
    public Transform mount;           // Điểm treo
    public Rigidbody2D bob;           // Vật nặng có Rigidbody2D
    public float length = 3f;         // Chiều dài dây
    public float maxLength = 4f;         // Chiều dài dây tối đa
    public float k = 50f;
    public float gravity = 9.81f;     // Gia tốc trọng trường (m/s²)
    public bool drawLine = true;
    public float damping = 0.999f;    // Giảm dao động nhẹ để tránh rung do sai số
    public float dist = 0f;

    private LineRenderer line;

    void Start()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 2;
        bob.gravityScale = 0f; // Tắt gravity mặc định, ta tự áp dụng để dễ kiểm soát
    }

    void FixedUpdate()
    {
        Vector2 pivot = mount.position;
        Vector2 pos = bob.position;
        Vector2 dir = pos - pivot;
        dist = dir.magnitude;

        // Áp dụng trọng lực thủ công
        bob.linearVelocity += Vector2.down * gravity * Time.fixedDeltaTime;

        if (Mathf.Abs(dist - maxLength) >= 0.01f)
        {
            Vector2 dirNorm = dir / dist;

            // Project vận tốc theo hướng dây
            float vAlongRope = Vector2.Dot(bob.linearVelocity, dirNorm);

            // Loại bỏ vận tốc hướng tâm, chỉ giữ thành phần tiếp tuyến
            bob.linearVelocity -= vAlongRope * dirNorm;

            // Kéo bob trở lại đúng độ dài dây
            Vector2 correctedPos = pivot + dirNorm * length;
            bob.position = correctedPos;
        }else

        if (dist > length && dist < maxLength)
        {
            Vector2 dirNorm = dir / dist;

            // Độ giãn của dây
            float stretch = dist - length;

            // Lực đàn hồi theo định luật Hooke
            Vector2 springForce = -k * stretch * dirNorm;

            // Áp dụng lực lên bob (gia tốc = lực / khối lượng)
            bob.linearVelocity += springForce / bob.mass * Time.fixedDeltaTime;

            // Giảm dao động hướng dọc dây (damping nội tại)
            float vAlongRope = Vector2.Dot(bob.linearVelocity, dirNorm);
            bob.linearVelocity -= vAlongRope * dirNorm * (1f - damping);
        }

        // Giảm năng lượng thừa nhẹ để tránh dao động vô hạn
        bob.linearVelocity *= damping;

        // Cập nhật LineRenderer
        if (drawLine)
        {
            line.SetPosition(0, pivot);
            line.SetPosition(1, bob.position);
        }
    }
}