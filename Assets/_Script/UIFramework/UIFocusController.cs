using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace UIFramework
{
    [DefaultExecutionOrder(60)]
    public class UIFocusController : MonoBehaviour
    {
        public static UIFocusController Instance { get; private set; }
        public EventSystem eventSystem;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            if (eventSystem == null) eventSystem = EventSystem.current;
        }

        public void SetSelected(GameObject go)
        {
            if (eventSystem == null || go == null) return;
            eventSystem.SetSelectedGameObject(go);
        }

        public void OnNavigate(InputAction.CallbackContext ctx)
        {
            // default: let EventSystem handle navigation
            // but we can implement custom logic if needed
        }

        public void OnSubmit(InputAction.CallbackContext ctx)
        {
            var selected = eventSystem?.currentSelectedGameObject;
            if (selected == null) return;
            var pointer = new PointerEventData(eventSystem) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(selected, pointer, ExecuteEvents.submitHandler);
        }

        public void OnCancel(InputAction.CallbackContext ctx)
        {
            // default behavior: close top screen if it allows
            var top = UIScreenManager.Instance?.TopScreen;
            if (top != null)
            {
                // use a well-known pattern: top's Cancel -> ask manager to Pop
                UIScreenManager.Instance.Pop();
            }
        }
    }
}
