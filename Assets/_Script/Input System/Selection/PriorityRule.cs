using UnityEngine;

namespace Game.Selection
{
    [CreateAssetMenu(menuName = "Game/Selection/Rules/Priority")]
    public sealed class PriorityRule : SelectionRule
    {
        public override float Evaluate(in SelectionContext context, ISelectable candidate)
        {
            return candidate.Priority * weight;
        }
    }
}
