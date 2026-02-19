using UnityEngine;
using Game.Selection;

namespace Game.Selection
{
    [DisallowMultipleComponent]
    public sealed class SelectionService2D : SelectionServiceBase
    {
        public float maxDistance = 50f;
        public LayerMask selectableMask = ~0;

        protected override void QueryCandidates(in SelectionContext context)
        {
            Vector3 worldPoint = context.Camera.ScreenToWorldPoint(context.ScreenPosition);
            var hits = Physics2D.OverlapCircleAll((Vector2)worldPoint, maxDistance, selectableMask);
            foreach (var c in hits)
            {
                var sel = c.GetComponentInParent<ISelectable>();
                if (sel != null && !candidates.Contains(sel)) candidates.Add(sel);
            }
        }
    }
}
