using Game.Selection;
using UnityEngine;

namespace Game.Interaction
{
    public readonly struct InteractionContext
    {
        public readonly GameObject Instigator;
        public readonly ISelectable Target;
        public readonly Vector3 Origin;
        public readonly float Timer;

        public InteractionContext(GameObject instigator, ISelectable target)
        {
            Instigator = instigator;
            Target = target;
            Origin = instigator != null ? instigator.transform.position : Vector3.zero;
            Timer = Time.time;
        }
    }
}
