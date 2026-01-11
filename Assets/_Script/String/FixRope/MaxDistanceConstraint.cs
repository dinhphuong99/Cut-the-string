using UnityEngine;

/// <summary>
/// Giới hạn khoảng cách giữa hai đối tượng không vượt quá maxDistance.
/// Thích hợp cho game logic, không cần mô phỏng vật lý.
/// </summary>
public class MaxDistanceConstraint : MonoBehaviour
{
    public Transform target;
    public float maxDistance = 1f;
    public bool usePhysicsCorrection = false;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 offset = transform.position - target.position;
        float distance = offset.magnitude;

        // Nếu vượt quá giới hạn thì ép về vị trí biên
        if (distance > maxDistance)
        {
            Vector3 correctedPos = target.position + offset.normalized * maxDistance;

            if (usePhysicsCorrection && rb != null)
            {
                rb.MovePosition(correctedPos); // Giữ tương thích hệ vật lý
            }
            else
            {
                transform.position = correctedPos; // Cập nhật trực tiếp
            }
        }
    }
}