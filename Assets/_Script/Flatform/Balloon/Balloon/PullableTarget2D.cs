using UnityEngine;

/// <summary>
/// PullableTarget2D:
/// - Đánh dấu object có thể được attach
/// - Quản lý priority
/// - Đảm bảo 1 target chỉ bị 1 balloon giữ
/// </summary>
public class PullableTarget2D : MonoBehaviour
{
    [Tooltip("Càng cao càng được ưu tiên.")]
    public int priority = 0;

    [Tooltip("Cho phép attach hay không.")]
    public bool canBePulled = true;

    public int TargetId { get; private set; }

    private BalloonAutoSwitch2D _currentOwner;

    private void Awake()
    {
        TargetId = GetInstanceID();
    }

    public bool IsAttachableBy(BalloonAutoSwitch2D balloon)
    {
        if (!canBePulled)
            return false;

        if (_currentOwner != null && _currentOwner != balloon)
            return false;

        return true;
    }

    public void OnAttached(BalloonAutoSwitch2D balloon)
    {
        _currentOwner = balloon;
    }

    public void OnDetached(BalloonAutoSwitch2D balloon)
    {
        if (_currentOwner == balloon)
            _currentOwner = null;
    }
}
