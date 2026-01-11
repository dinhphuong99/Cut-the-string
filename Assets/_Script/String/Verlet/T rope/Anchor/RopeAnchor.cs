using UnityEngine;
using System;

[DisallowMultipleComponent]
public class RopeAnchor : MonoBehaviour, IRopeAnchor
{
    public Transform Transform => transform;

    public bool IsActive => isActiveAndEnabled;

    public event Action<IRopeAnchor> OnAnchorInvalidated;

    private void OnEnable()
    {
        RopeAnchorRegistry.Register(this);
    }

    private void OnDisable()
    {
        Invalidate();
        RopeAnchorRegistry.Unregister(this);
    }

    private void OnDestroy()
    {
        Invalidate();
        RopeAnchorRegistry.Unregister(this);
    }

    private void Invalidate()
    {
        OnAnchorInvalidated?.Invoke(this);
    }
}