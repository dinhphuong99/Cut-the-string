using UnityEngine;

namespace Game.Selection
{
    public abstract class SelectionRule : ScriptableObject
    {
        public float weight = 1f;
        public abstract float Evaluate(in SelectionContext context, ISelectable candidate);
    }
}
