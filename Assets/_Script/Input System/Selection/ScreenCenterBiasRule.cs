using UnityEngine;

namespace Game.Selection
{
    [CreateAssetMenu(menuName = "Selection/Rules/ScreenCenterBias")]
    public class ScreenCenterBiasRule : SelectionRule
    {
        [Tooltip("radius in pixels where bias applies")]
        public float maxRadiusPixels = 200f;

        public override float Evaluate(in SelectionContext ctx, ISelectable candidate)
        {
            if (ctx.Camera == null) return 0f;
            Vector3 screenPos = ctx.Camera.WorldToScreenPoint(candidate.Transform.position);
            float distPx = Vector2.Distance(new Vector2(screenPos.x, screenPos.y), ctx.ScreenPosition);
            float t = Mathf.Clamp01(1f - (distPx / maxRadiusPixels));
            return t * weight;
        }
    }
}