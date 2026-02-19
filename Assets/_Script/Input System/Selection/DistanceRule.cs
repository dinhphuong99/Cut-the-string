using UnityEngine;

namespace Game.Selection
{
    [CreateAssetMenu(menuName = "Game/Selection/Rules/Distance")]
    public sealed class DistanceRule : SelectionRule
    {
        public float maxDistance = 20f;

        public override float Evaluate(in SelectionContext context, ISelectable candidate)
        {
            if (context.Camera == null) return 0f;
            float d = Vector3.Distance(context.Camera.transform.position, candidate.Transform.position);
            if (d > maxDistance) return -1000f;
            return (1f - (d / maxDistance)) * weight;
        }
    }
}
