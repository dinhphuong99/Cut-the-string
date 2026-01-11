using UnityEngine;

/// <summary>
/// Mô phỏng con lắc đàn hồi (dây có thể giãn như lò xo).
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class ElasticPendulum : MonoBehaviour
{
    public Transform mount;            // Điểm treo
    public Rigidbody2D bob;            // Vật nặng có Rigidbody2D
    public float restLength = 3f;      // Chiều dài nghỉ của dây
    public float stiffness = 50f;      // Độ cứng dây (Hooke's constant)
    public float damping = 0.98f;      // Hệ số giảm dao động
    public float gravity = 9.81f;      // Gia tốc trọng trường
    public bool drawLine = true;

    private LineRenderer line;

    void Start()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 2;
        bob.gravityScale = 0f; // Tự áp dụng trọng lực
    }

    void FixedUpdate()
    {
        Vector2 pivot = mount.position;
        Vector2 pos = bob.position;
        Vector2 dir = pos - pivot;
        float dist = dir.magnitude;
        if (dist == 0) return;

        Vector2 dirNorm = dir / dist;

        // Trọng lực
        Vector2 force = Vector2.down * gravity * bob.mass;

        // Lực đàn hồi dây (chỉ khi dây giãn)
        float stretch = dist - restLength;
        if (stretch > 0f)
        {
            force += -dirNorm * (stretch * stiffness);
        }

        // Cộng lực tổng vào Rigidbody
        bob.AddForce(force);

        // Giảm vận tốc để ổn định
        bob.linearVelocity *= damping;

        // Vẽ dây
        if (drawLine)
        {
            line.SetPosition(0, pivot);
            line.SetPosition(1, pos);
        }
    }
}