using UnityEngine;

namespace Game.Selection
{
    [CreateAssetMenu(menuName = "Game/Selection/Rules/Occlusion")]
    public sealed class OcclusionRule : SelectionRule
    {
        public LayerMask occlusionMask;
        public bool useCameraRay = true;

        public override float Evaluate(in SelectionContext context, ISelectable candidate)
        {
            if (context.Camera == null) return 0f;
            Vector3 from = useCameraRay ? context.Camera.transform.position : context.Camera.ScreenPointToRay(context.ScreenPosition).origin;
            Vector3 to = candidate.Transform.position;
            Vector3 dir = to - from;
            float dist = dir.magnitude;
            if (dist <= 0.001f) return 0f;
            if (Physics.Raycast(from, dir.normalized, out var hit, dist, occlusionMask))
                return -1000f;
            return 0f;
        }
    }
}
