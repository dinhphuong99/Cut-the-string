using UnityEngine;

namespace Game.Selection
{
    [DisallowMultipleComponent]
    public sealed class SelectionService3D : SelectionServiceBase
    {
        public float maxDistance = 50f;
        public LayerMask selectableMask = ~0;

        protected override void QueryCandidates(in SelectionContext context)
        {
            Ray ray = context.Camera.ScreenPointToRay(context.ScreenPosition);
            var hits = Physics.RaycastAll(ray, maxDistance, selectableMask);
            foreach (var hit in hits)
            {
                var sel = hit.collider.GetComponentInParent<ISelectable>();
                if (sel != null && !candidates.Contains(sel)) candidates.Add(sel);
            }
        }
    }
}
