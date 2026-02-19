using UnityEngine;

/// <summary>
/// BalloonRopeRenderer2D:
/// - Vẽ dây nối giữa Balloon và target khi attach
/// - Ẩn dây khi detach
/// - Không can thiệp gameplay hay physics
/// </summary>
[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(BalloonJoint2D))]
public class BalloonRopeRenderer2D : MonoBehaviour
{
    [Header("Rope Settings")]
    [SerializeField] private int segmentCount = 2; // 2 = dây thẳng
    [SerializeField] private float slack = 0f;      // Để sau này mở rộng (rope cong)

    private LineRenderer _line;
    private BalloonJoint2D _joint;

    private void Awake()
    {
        _line = GetComponent<LineRenderer>();
        _joint = GetComponent<BalloonJoint2D>();

        _line.positionCount = segmentCount;
        _line.enabled = false;
        _line.useWorldSpace = true;
    }

    private void LateUpdate()
    {
        if (!_joint.IsAttached || _joint.CurrentTarget == null)
        {
            if (_line.enabled)
                _line.enabled = false;

            return;
        }

        if (!_line.enabled)
            _line.enabled = true;

        UpdateLinePositions();
    }

    private void UpdateLinePositions()
    {
        Vector3 start = transform.position;
        Vector3 end = _joint.CurrentTarget.transform.position;

        if (segmentCount <= 2)
        {
            // Dây thẳng
            _line.SetPosition(0, start);
            _line.SetPosition(1, end);
            return;
        }

        // Chuẩn bị cho rope cong (để extensible, chưa cần dùng ngay)
        for (int i = 0; i < segmentCount; i++)
        {
            float t = i / (float)(segmentCount - 1);
            Vector3 pos = Vector3.Lerp(start, end, t);

            // Slack đơn giản theo trục Y
            if (slack > 0f)
            {
                float curve = Mathf.Sin(t * Mathf.PI) * slack;
                pos.y -= curve;
            }

            _line.SetPosition(i, pos);
        }
    }
}
