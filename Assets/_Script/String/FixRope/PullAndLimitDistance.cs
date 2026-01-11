using UnityEngine;

/// <summary>
/// Một object có thể kéo object còn lại, và khoảng cách giữa hai object không bao giờ vượt quá maxDistance.
/// Tự động thích ứng nếu một hoặc cả hai có Rigidbody.
/// </summary>
public class PullAndLimitDistance : MonoBehaviour
{
    [Header("Setup")]
    public Transform target;
    public float maxDistance = 2f;
    public float pullStrength = 10f; // độ mạnh khi kéo object kia
    public bool bidirectional = false; // nếu true: cả hai cùng chịu tác động khi quá xa

    private Rigidbody rbSelf;
    private Rigidbody rbTarget;

    void Awake()
    {
        rbSelf = GetComponent<Rigidbody>();
        rbTarget = target != null ? target.GetComponent<Rigidbody>() : null;
    }

    void FixedUpdate()
    {
        if (target == null) return;

        Vector3 offset = target.position - transform.position;
        float distance = offset.magnitude;
        if (distance <= maxDistance) return;

        Vector3 direction = offset.normalized;
        float excess = distance - maxDistance;

        // Tính vị trí mong muốn để không vượt giới hạn
        Vector3 correction = direction * excess;

        // Nếu cả hai không có Rigidbody => di chuyển trực tiếp
        if (rbSelf == null && rbTarget == null)
        {
            target.position -= correction;
            return;
        }

        // Nếu có Rigidbody thì áp dụng lực kéo (dạng mềm)
        Vector3 pullForce = direction * (excess * pullStrength);

        if (bidirectional)
        {
            if (rbSelf != null) rbSelf.AddForce(pullForce, ForceMode.Acceleration);
            if (rbTarget != null) rbTarget.AddForce(-pullForce, ForceMode.Acceleration);
        }
        else
        {
            // Một chiều: chỉ object này kéo target
            if (rbTarget != null)
                rbTarget.AddForce(-pullForce, ForceMode.Acceleration);
            else
                target.position -= correction; // nếu target không có Rigidbody → dịch trực tiếp
        }
    }
}