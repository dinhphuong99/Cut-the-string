using UnityEngine;

/// <summary>
/// Giữ khoảng cách TỐI THIỂU giữa hai đối tượng.
/// Nếu khoảng cách nhỏ hơn minDistance, object sẽ đẩy target ra xa.
/// Tự thích ứng cho cả Rigidbody hoặc không.
/// </summary>
public class PushAndLimitMinDistance : MonoBehaviour
{
    [Header("Setup")]
    public Transform target;
    public float minDistance = 1f;
    public float pushStrength = 10f;  // lực đẩy khi object kia quá gần
    public bool bidirectional = false; // nếu true: cả hai cùng bị đẩy

    private Rigidbody rbSelf;
    private Rigidbody rbTarget;

    void Awake()
    {
        rbSelf = GetComponent<Rigidbody>();
        if (target != null)
            rbTarget = target.GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (target == null) return;

        Vector3 offset = target.position - transform.position;
        float distance = offset.magnitude;
        if (distance >= minDistance || distance <= Mathf.Epsilon) return;

        Vector3 direction = offset.normalized;
        float overlap = minDistance - distance;

        // Nếu cả hai không có Rigidbody → dịch trực tiếp
        if (rbSelf == null && rbTarget == null)
        {
            target.position = transform.position + direction * minDistance;
            return;
        }

        // Tính lực đẩy (theo hướng ra xa)
        Vector3 pushForce = -direction * (overlap * pushStrength);

        if (bidirectional)
        {
            // Cả hai cùng bị đẩy ra xa nhau
            if (rbSelf != null) rbSelf.AddForce(pushForce, ForceMode.Acceleration);
            if (rbTarget != null) rbTarget.AddForce(-pushForce, ForceMode.Acceleration);
        }
        else
        {
            // Một chiều: object này đẩy target ra
            if (rbTarget != null)
                rbTarget.AddForce(-pushForce, ForceMode.Acceleration);
            else
                target.position = transform.position + direction * minDistance;
        }
    }
}