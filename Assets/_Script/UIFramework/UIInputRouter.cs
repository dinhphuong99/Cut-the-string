using UnityEngine;
using UnityEngine.InputSystem;

namespace UIFramework
{
    [DefaultExecutionOrder(50)]
    public sealed class UIInputRouter : MonoBehaviour
    {
        [Header("Input references (InputActionReference from Input System)")]
        [SerializeField] private InputActionReference navigate;
        [SerializeField] private InputActionReference submit;
        [SerializeField] private InputActionReference cancel;
        [SerializeField] private InputActionReference point; // pointer position / click (optional)

        [Header("Behavior")]
        [Tooltip("If true: will still route input even if no TopScreen exists (useful for debug overlays).")]
        [SerializeField] private bool allowInputWithoutScreen = false;

        [Tooltip("Log warnings if bindings are missing. Recommended ON during development.")]
        [SerializeField] private bool logMissingBindings = true;

        private bool _isBound;

        private void OnEnable()
        {
            BindIfNeeded();
            EnableActions();
        }

        private void OnDisable()
        {
            DisableActions();
            UnbindIfNeeded();
        }

        private void BindIfNeeded()
        {
            if (_isBound) return;

            TryBind(navigate, OnNavigate, nameof(navigate));
            TryBind(submit, OnSubmit, nameof(submit));
            TryBind(cancel, OnCancel, nameof(cancel));
            TryBind(point, OnPoint, nameof(point)); // optional

            _isBound = true;
        }

        private void UnbindIfNeeded()
        {
            if (!_isBound) return;

            TryUnbind(navigate, OnNavigate);
            TryUnbind(submit, OnSubmit);
            TryUnbind(cancel, OnCancel);
            TryUnbind(point, OnPoint);

            _isBound = false;
        }

        private void EnableActions()
        {
            TryEnable(navigate);
            TryEnable(submit);
            TryEnable(cancel);
            TryEnable(point);
        }

        private void DisableActions()
        {
            TryDisable(navigate);
            TryDisable(submit);
            TryDisable(cancel);
            TryDisable(point);
        }

        private void TryBind(InputActionReference reference, System.Action<InputAction.CallbackContext> handler, string fieldName)
        {
            if (reference == null || reference.action == null)
            {
                if (logMissingBindings)
                    Debug.LogWarning($"{nameof(UIInputRouter)} on '{name}': Missing InputActionReference for '{fieldName}'.", this);
                return;
            }

            // IMPORTANT: event only supports += / -=
            reference.action.performed += handler;
        }

        private void TryUnbind(InputActionReference reference, System.Action<InputAction.CallbackContext> handler)
        {
            if (reference == null || reference.action == null) return;
            reference.action.performed -= handler;
        }

        private static void TryEnable(InputActionReference reference)
        {
            if (reference == null || reference.action == null) return;
            reference.action.Enable();
        }

        private static void TryDisable(InputActionReference reference)
        {
            if (reference == null || reference.action == null) return;
            reference.action.Disable();
        }

        private bool CanRouteUIInput()
        {
            // Nếu bạn có UIScreenManager, đây là gate hợp lý.
            var manager = UIScreenManager.Instance;
            if (manager == null)
                return allowInputWithoutScreen;

            if (allowInputWithoutScreen)
                return true;

            return manager.TopScreen != null;
        }

        private UIFocusController Focus => UIFocusController.Instance;

        private void OnNavigate(InputAction.CallbackContext ctx)
        {
            if (!CanRouteUIInput()) return;

            // Navigate thường là Vector2
            // var move = ctx.ReadValue<Vector2>();

            Focus?.OnNavigate(ctx);
        }

        private void OnSubmit(InputAction.CallbackContext ctx)
        {
            if (!CanRouteUIInput()) return;
            Focus?.OnSubmit(ctx);
        }

        private void OnCancel(InputAction.CallbackContext ctx)
        {
            if (!CanRouteUIInput()) return;
            Focus?.OnCancel(ctx);
        }

        private void OnPoint(InputAction.CallbackContext ctx)
        {
            // Optional:
            // Nếu bạn muốn “click để focus”, bạn có thể route vào focus controller.
            // Nhưng thường pointer sẽ do Unity UI EventSystem xử lý (InputSystemUIInputModule).
            //
            // Focus?.OnPoint(ctx);
        }
    }
}
