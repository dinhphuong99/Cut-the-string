using UnityEngine;

/// <summary>
/// Mô phỏng con lắc đơn bằng lực thực tế, dây không co giãn.
/// Giữ chiều dài dây cố định bằng ràng buộc vị trí và vận tốc.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class PendulumForce : MonoBehaviour
{
    public Transform mount;           // Điểm treo
    public Rigidbody2D bob;           // Vật nặng có Rigidbody2D
    public float length = 3f;         // Chiều dài dây
    public float gravity = 9.81f;     // Gia tốc trọng trường (m/s²)
    public bool drawLine = true;
    public float damping = 0.999f;    // Giảm dao động nhẹ để tránh rung do sai số

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
        float dist = dir.magnitude;

        // Áp dụng trọng lực thủ công
        bob.linearVelocity += Vector2.down * gravity * Time.fixedDeltaTime;

        // Ràng buộc chiều dài dây (không cho co giãn)
        if (dist != 0)
        {
            Vector2 dirNorm = dir / dist;

            // Project vận tốc theo hướng dây
            float vAlongRope = Vector2.Dot(bob.linearVelocity, dirNorm);

            // Loại bỏ vận tốc hướng tâm, chỉ giữ thành phần tiếp tuyến
            bob.linearVelocity -= vAlongRope * dirNorm;

            // Kéo bob trở lại đúng độ dài dây
            Vector2 correctedPos = pivot + dirNorm * length;
            bob.position = correctedPos;
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