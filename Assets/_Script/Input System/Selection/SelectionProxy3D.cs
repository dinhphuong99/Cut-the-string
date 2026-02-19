using UnityEngine;

namespace Game.Selection
{
    [RequireComponent(typeof(Collider))]
    public sealed class SelectionProxy3D : MonoBehaviour
    {
        [Tooltip("Assign a MonoBehaviour that implements ISelectable (owner).")]
        public MonoBehaviour ownerBehaviour;

        [Tooltip("Proxy-level override priority; added to owner's SelectionPriority.")]
        public int customPriority = 0;

        [Tooltip("Set proxy collider to trigger to avoid physics responses")]
        public bool isTriggerProxy = true;

        public ISelectable Owner => ownerBehaviour as ISelectable;

        void Reset()
        {
            var c = GetComponent<Collider>();
            if (c != null) c.isTrigger = isTriggerProxy;
        }
    }
}