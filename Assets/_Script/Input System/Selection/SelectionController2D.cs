using UnityEngine;
using Game.Interaction;
using Game.Selection;

[DisallowMultipleComponent]
public sealed class SelectionController2D : SelectionControllerBase
{
    [SerializeField] private LayerMask selectableMask;

    private Vector2 lastPoint;

    protected override void TrySelect(Vector2 screenPosition)
    {
        Vector3 screen = new Vector3(
            screenPosition.x,
            screenPosition.y,
            Mathf.Abs(mainCamera.transform.position.z)
        );

        Vector3 world = mainCamera.ScreenToWorldPoint(screen);
        lastPoint = world;

        Collider2D hit = Physics2D.OverlapPoint(world, selectableMask);

        if (hit == null)
        {
            ClearSelection();
            return;
        }

        GameObject selectedObject = ResolveRedirect(hit.gameObject);
        SetSelected(selectedObject);
    }

    private void ClearSelection()
    {
        if (CurrentSelected == null)
            return;

        if (CurrentSelected.TryGetComponent<ISelectable>(out var prev))
            prev.OnDeselected();

        CurrentSelected = null;
    }


    private GameObject ResolveRedirect(GameObject hitObject)
    {
        // Try get redirect handler
        if (hitObject.TryGetComponent<IInteractionRedirect>(out var redirect))
        {
            GameObject target = redirect.GetInteractionTarget();

            if (target != null)
                return target;
        }

        return hitObject;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(lastPoint, 0.45f);
    }
}