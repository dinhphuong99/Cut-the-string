using UnityEngine;

namespace Game.Input
{
    [DisallowMultipleComponent]
    public sealed class JoystickInputSource : MonoBehaviour, IInputSource
    {
        [SerializeField] private VirtualJoystick joystick;
        [Tooltip("Action id string matching InputActionKey.Id and InputActions asset action name.")]
        [SerializeField] private string actionId = "Move";
        private InputActionKey _key;
        [SerializeField] private string visibleContextId = "Gameplay";

        private void Awake()
        {
            InputRegistry.Initialize();
            _key = InputRegistry.GetActionKey(actionId);
            if (joystick == null) { Debug.LogError("[JoystickInputSource] joystick missing"); enabled = false; }
            if (_key == null) Debug.LogWarning($"[JoystickInputSource] action key '{actionId}' not found.");
        }

        public void UpdateIntent(InputIntent intent)
        {
            if (joystick == null || _key == null) return;
            var dir = joystick.Direction;
            if (dir != Vector2.zero) intent.Set(_key, InputActionValue.FromVector(dir));
        }

        public void OnContextChanged(InputContextKey newContext)
        {
            var visible = newContext != null && newContext.Id == visibleContextId;
            gameObject.SetActive(visible);
        }
    }
}
