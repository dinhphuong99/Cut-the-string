using UnityEngine;
using Game.Selection;

[DisallowMultipleComponent]
public sealed class SelectionController3D : SelectionControllerBase
{
    [SerializeField] private LayerMask selectableMask;
    [SerializeField] private LayerMask occluderMask;
    [SerializeField] private float maxDistance = 1000f;

    protected override void TrySelect(Vector2 screenPosition)
    {
        if (mainCamera == null)
            return;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            maxDistance,
            selectableMask | occluderMask,
            QueryTriggerInteraction.Ignore
        );

        if (hits.Length == 0)
        {
            SetSelected(null);
            return;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            GameObject go = hit.collider.gameObject;

            // Occluder blocks ray
            if (go.TryGetComponent<IOccluder>(out var occluder)
                && occluder.BlocksSelection)
            {
                SetSelected(null);
                return;
            }

            // Selectable found
            if (go.TryGetComponent<ISelectable>(out _))
            {
                SetSelected(go);
                return;
            }
        }

        SetSelected(null);
    }
}
