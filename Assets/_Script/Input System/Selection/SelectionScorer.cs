using System.Collections.Generic;
using UnityEngine;

namespace Game.Selection
{
    [CreateAssetMenu(menuName = "Game/Selection/Scorer")]
    public sealed class SelectionScorer : ScriptableObject
    {
        public List<SelectionRule> rules = new();

        public float Score(in SelectionContext context, ISelectable candidate)
        {
            if (candidate == null || !candidate.IsSelectable) return float.NegativeInfinity;
            float total = 0f;
            foreach (var r in rules)
            {
                if (r == null) continue;
                total += r.Evaluate(context, candidate);
            }
            return total;
        }
    }
}
