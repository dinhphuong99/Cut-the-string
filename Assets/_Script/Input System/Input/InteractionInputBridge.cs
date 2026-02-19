using UnityEngine;
using Game.Selection;
using Game.Interaction;

namespace Game.Input
{
    [DisallowMultipleComponent]
    public sealed class InteractionInputBridge : MonoBehaviour
    {
        [SerializeField] private InputHub hub;
        [SerializeField] private SelectionServiceBase selectionService;
        [SerializeField] private string confirmActionId = "Interact";
        [SerializeField] private float interactionCooldown = 0.25f;

        private InteractionService _interactionService;
        private InputActionKey _confirmKey;

        private void Awake()
        {
            InputRegistry.Initialize();
            if (hub == null) hub = FindObjectOfType<InputHub>();
            if (hub == null) Debug.LogError("[InteractionInputBridge] InputHub not found.");
            if (selectionService == null) Debug.LogError("[InteractionInputBridge] selection service not assigned.");

            _interactionService = new InteractionService(selectionService, interactionCooldown);
            _confirmKey = InputRegistry.GetActionKey(confirmActionId);
            if (_confirmKey == null) Debug.LogWarning($"[InteractionInputBridge] InputActionKey '{confirmActionId}' not found.");
        }

        private void Update()
        {
            if (hub == null || _confirmKey == null) return;
            if (hub.Current.TryGet(_confirmKey, out var val) && val.Bool)
                _interactionService.TryInteract(gameObject);
        }
    }
}
