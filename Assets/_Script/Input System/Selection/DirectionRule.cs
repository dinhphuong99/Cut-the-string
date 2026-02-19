using UnityEngine;

namespace Game.Selection
{
    [CreateAssetMenu(menuName = "Game/Selection/Rules/Direction")]
    public sealed class DirectionRule : SelectionRule
    {
        public float minDot = -1f;

        public override float Evaluate(in SelectionContext context, ISelectable candidate)
        {
            if (context.Camera == null) return 0f;
            Vector3 toTarget = (candidate.Transform.position - context.Camera.transform.position).normalized;
            float dot = Vector3.Dot(context.Camera.transform.forward, toTarget);
            if (dot < minDot) return -1000f;
            return Mathf.Clamp01((dot + 1f) * 0.5f) * weight;
        }
    }
}
