using UnityEngine;

/// <summary>
/// BalloonJoint2D:
/// - Quản lý DistanceJoint2D
/// - Attach vào GameObject bất kỳ
///   + Có Rigidbody2D  -> connectedBody
///   + Không có        -> connectedAnchor (world)
/// - Không biết priority, anchor, hay gameplay
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BalloonJoint2D : MonoBehaviour
{
    [SerializeField] private float minDistance = 0.01f;

    private Rigidbody2D _rb;
    private DistanceJoint2D _joint;

    private Transform _targetTransform;
    private Rigidbody2D _targetRb;

    public bool IsAttached => _joint.enabled;
    public GameObject CurrentTarget => _targetTransform ? _targetTransform.gameObject : null;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        _joint = gameObject.AddComponent<DistanceJoint2D>();
        _joint.autoConfigureDistance = false;
        _joint.enableCollision = true;
        _joint.enabled = false;
    }

    private void FixedUpdate()
    {
        // Nếu target KHÔNG có Rigidbody2D thì phải update anchor theo transform
        if (_joint.enabled && _targetRb == null && _targetTransform != null)
        {
            _joint.connectedAnchor = _targetTransform.position;
        }
    }

    public void Attach(GameObject target)
    {
        if (target == null)
            return;

        _targetTransform = target.transform;
        _targetRb = target.GetComponent<Rigidbody2D>();

        Vector2 targetPos = _targetTransform.position;
        float dist = Vector2.Distance(_rb.position, targetPos);
        _joint.distance = Mathf.Max(dist, minDistance);

        if (_targetRb != null)
        {
            _joint.connectedBody = _targetRb;
        }
        else
        {
            _joint.connectedBody = null;
            _joint.connectedAnchor = targetPos;
        }

        _joint.enabled = true;
    }

    public void Detach()
    {
        _joint.enabled = false;
        _joint.connectedBody = null;
        _targetTransform = null;
        _targetRb = null;
    }

    private void OnDisable()
    {
        Detach();
    }
}
