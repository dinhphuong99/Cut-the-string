using UnityEngine;

namespace Game.Selection
{
    [DisallowMultipleComponent]
    public sealed class SelectableObject : MonoBehaviour, ISelectable
    {
        [Tooltip("Higher = higher priority")]
        public int priority = 0;
        public bool selectable = true;

        public Transform Transform => transform;
        public int Priority => priority;
        public bool IsSelectable => selectable;

        public void OnSelected() { /* highlight */ }
        public void OnDeselected() { /* remove highlight */ }
    }
}
