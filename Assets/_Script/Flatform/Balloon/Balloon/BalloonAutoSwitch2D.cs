using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BalloonAutoSwitch2D:
/// - Quét PullableTarget2D trong range
/// - Rule A: priority cao hơn thắng, nếu bằng -> gần hơn
/// - Không attach lại target mà balloon này đã detach
/// - Target đang bị balloon khác giữ -> bỏ
/// </summary>
[RequireComponent(typeof(BalloonJoint2D))]
public class BalloonAutoSwitch2D : MonoBehaviour
{
    [SerializeField] private float detectRange = 2f;
    [SerializeField] private LayerMask targetLayer;

    [Header("Stability")]
    [SerializeField] private float switchCooldown = 0.25f;
    [SerializeField] private int priorityHysteresis = 0;

    private BalloonJoint2D _joint;

    private readonly Collider2D[] _buffer = new Collider2D[32];
    private readonly HashSet<int> _detachedIds = new();

    private float _nextSwitchTime;

    private PullableTarget2D _currentTarget;

    private void Awake()
    {
        _joint = GetComponent<BalloonJoint2D>();
    }

    private void FixedUpdate()
    {
        if (Time.time < _nextSwitchTime)
            return;

        PullableTarget2D best = PickBestTarget();

        if (best != null && best != _currentTarget)
        {
            if (_currentTarget != null)
            {
                int delta = best.priority - _currentTarget.priority;
                if (delta < priorityHysteresis)
                    return;

                DetachCurrent();
            }

            AttachTo(best);
        }
        else if (best == null && _currentTarget != null)
        {
            DetachCurrent();
        }
    }

    private void AttachTo(PullableTarget2D target)
    {
        _currentTarget = target;
        target.OnAttached(this);

        _joint.Attach(target.gameObject);
        _nextSwitchTime = Time.time + switchCooldown;
    }

    private void DetachCurrent()
    {
        if (_currentTarget == null)
            return;

        _currentTarget.OnDetached(this);
        _detachedIds.Add(_currentTarget.TargetId);

        _currentTarget = null;
        _joint.Detach();
        _nextSwitchTime = Time.time + switchCooldown;
    }

    private PullableTarget2D PickBestTarget()
    {
        int count = Physics2D.OverlapCircleNonAlloc(
            transform.position,
            detectRange,
            _buffer,
            targetLayer
        );

        PullableTarget2D best = null;
        int bestPriority = int.MinValue;
        float bestDist = float.PositiveInfinity;
        Vector2 origin = transform.position;

        for (int i = 0; i < count; i++)
        {
            Collider2D col = _buffer[i];
            if (col == null) continue;

            if (!col.TryGetComponent(out PullableTarget2D marker))
                continue;

            if (_detachedIds.Contains(marker.TargetId))
                continue;

            if (!marker.IsAttachableBy(this))
                continue;

            float d = Vector2.Distance(origin, col.transform.position);
            int p = marker.priority;

            if (p > bestPriority || (p == bestPriority && d < bestDist))
            {
                best = marker;
                bestPriority = p;
                bestDist = d;
            }
        }

        return best;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
#endif
}
