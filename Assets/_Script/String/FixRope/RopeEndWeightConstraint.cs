using UnityEngine;

/// <summary>
/// Giả lập trọng lực cho endPoint của RopeFixBehaviour,
/// đồng thời giới hạn khoảng cách giữa hai đầu dây.
/// Không dùng Rigidbody, hoàn toàn bằng tính toán thủ công.
/// </summary>

public class RopeEndWeightConstraint : MonoBehaviour
{
    private void FixedUpdate()
    {
        transform.position = transform.position + Vector3.down.normalized;
    }
}