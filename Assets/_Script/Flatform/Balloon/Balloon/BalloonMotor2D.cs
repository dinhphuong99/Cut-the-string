using UnityEngine;

/// <summary>
/// BalloonMotor2D chỉ lo chuyển động của Balloon:
/// - Lift bay lên
/// - Điều khiển ngang nhẹ
/// 
/// Không đụng gì tới logic attach để dễ maintain.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BalloonMotor2D : MonoBehaviour
{
    [Header("Lift (Buoyancy)")]
    [SerializeField] private float liftForce = 25f;
    [SerializeField] private float maxUpSpeed = 4f;


    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        ApplyLift();
    }

    private void ApplyLift()
    {
        // Không cap tốc độ => balloon tăng tốc vô hạn => joint giật, rung, nổ solver.
        if (_rb.linearVelocity.y < maxUpSpeed)
        {
            _rb.AddForce(Vector2.up * liftForce, ForceMode2D.Force);
        }
    }
}