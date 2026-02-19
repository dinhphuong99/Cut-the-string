using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Input
{
    [RequireComponent(typeof(PlayerInput))]
    public sealed class InputSystemSource : MonoBehaviour, IInputSource
    {
        [SerializeField] private PlayerInput playerInput;
        private readonly Dictionary<string, InputActionKey> _actionNameToKey = new();

        private void Awake()
        {
            InputRegistry.Initialize();
            if (playerInput == null) playerInput = GetComponent<PlayerInput>();
            if (playerInput == null)
            {
                Debug.LogError("[InputSystemSource] PlayerInput missing.");
                enabled = false;
                return;
            }

            foreach (var action in playerInput.actions)
            {
                if (action == null || string.IsNullOrEmpty(action.name)) continue;
                var key = InputRegistry.GetActionKey(action.name);
                if (key != null) _actionNameToKey[action.name] = key;
                else Debug.Log($"[InputSystemSource] No InputActionKey asset for action '{action.name}'. Create under Resources/InputKeys.");
            }
        }

        public void UpdateIntent(InputIntent intent)
        {
            if (playerInput == null) return;
            foreach (var action in playerInput.actions)
            {
                if (action == null) continue;
                if (!_actionNameToKey.TryGetValue(action.name, out var key)) continue;

                // heuristics: Vector2 -> float -> button
                if (action.activeControl != null && action.activeControl.valueType == typeof(Vector2))
                {
                    var v = action.ReadValue<Vector2>();
                    intent.Set(key, InputActionValue.FromVector(v));
                }
                else if (action.activeControl != null && action.activeControl.valueType == typeof(float))
                {
                    var f = action.ReadValue<float>();
                    intent.Set(key, InputActionValue.FromFloat(f));
                }
                else
                {
                    if (action.triggered)
                        intent.Set(key, InputActionValue.FromButton(true));
                }
            }
        }

        public void OnContextChanged(InputContextKey newContext)
        {
            if (playerInput != null && newContext != null)
                playerInput.SwitchCurrentActionMap(newContext.Id);
        }
    }
}
