using UnityEngine;

public class RopePendulumBinder : MonoBehaviour
{
    [SerializeField] private VerletRope7 rope;
    [SerializeField] private PendulumSpringForceProfiled2 pendulum;

    void LateUpdate()
    {
        if (!Validate())
        {
            pendulum.enabled = false;
            return;
        }

        Sync();
    }

    private bool Validate()
    {
        if (rope == null || pendulum == null)
            return false;

        if (rope.startPoint == null || rope.endPoint == null)
            return false;

        return true;
    }

    private void Sync()
    {
        // Lấy anchor từ rope
        Transform anchor = rope.startPoint;

        // Nếu endPoint có Rigidbody2D thì bỏ lực kéo từ rope
        Rigidbody2D bobRb = rope.endPoint != null ? rope.endPoint.GetComponent<Rigidbody2D>() : null;

        // Tính chiều dài lý tưởng
        float L = rope.GetLengthAtStretch(0.2f);

        // Cập nhật pendulum bằng 3 tham số này
        pendulum.SetExternalParams(anchor, bobRb, L);
    }

}
