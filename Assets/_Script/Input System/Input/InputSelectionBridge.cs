using UnityEngine;
using Game.Selection;

namespace Game.Input
{
    [DisallowMultipleComponent]
    public sealed class InputSelectionBridge : MonoBehaviour
    {
        [SerializeField] private InputHub hub;
        [SerializeField] private SelectionServiceBase selectionServiceBehaviour; // assign SelectionService2D or 3D

        private void Awake()
        {
            if (hub == null) hub = FindObjectOfType<InputHub>();
            if (hub == null) Debug.LogError("[InputSelectionBridge] InputHub not found");
            if (selectionServiceBehaviour == null) Debug.LogError("[InputSelectionBridge] selection service not assigned");
        }

        private void Update()
        {
            if (hub == null || selectionServiceBehaviour == null) return;
            var key = InputRegistry.GetActionKey("Select");
            if (key == null) return;
            if (hub.Current.TryGet(key, out var val))
                selectionServiceBehaviour.RequestSelection(val.Vector);
        }
    }
}
