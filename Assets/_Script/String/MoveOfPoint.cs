using UnityEngine;

[System.Serializable]
public class MoveOfPoint : MonoBehaviour
{
    [Header("Physics")]
    public float mass = 1f;                  // Khối lượng của node
    public bool isFixed = false;             // Nếu true thì node này không di chuyển (đầu dây cố định)

    [HideInInspector] public Vector3 velocity;   // Vận tốc hiện tại
    [HideInInspector] public Vector3 totalForce; // Tổng lực được áp trong frame hiện tại

    void Awake()
    {
        velocity = Vector3.zero;
        totalForce = Vector3.zero;
    }

    public void Integrate(float deltaTime, float damping)
    {
        if (isFixed) return;

        // Cập nhật vận tốc và vị trí theo lực hiện tại
        Vector3 acceleration = totalForce / mass;
        velocity += acceleration * deltaTime;
        velocity *= (1f - damping * deltaTime);
        transform.position += velocity * deltaTime;

        // Reset lực sau mỗi frame
        totalForce = Vector3.zero;
    }

    public void ApplyForce(Vector3 force)
    {
        if (!isFixed)
            totalForce += force;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isFixed ? Color.red : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.05f);
    }
}