using Game.Selection;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class SelectionControllerBase : MonoBehaviour
{
    [SerializeField] protected Camera mainCamera;
    [SerializeField] protected InputReader inputReader;

    public GameObject CurrentSelected { get; protected set; }

    protected virtual void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (inputReader == null)
            inputReader = FindObjectOfType<InputReader>();
    }

    protected virtual void OnEnable()
    {
        inputReader.Actions.Gameplay.PrimaryInteract.performed += OnPrimaryInteract;
    }

    protected virtual void OnDisable()
    {
        inputReader.Actions.Gameplay.PrimaryInteract.performed -= OnPrimaryInteract;
    }

    private void OnPrimaryInteract(InputAction.CallbackContext ctx)
    {
        Vector2 screenPos = GetPointerScreenPosition();
        TrySelect(screenPos);
    }

    protected Vector2 GetPointerScreenPosition()
    {
        if (Pointer.current == null)
            return Vector2.zero;

        return Pointer.current.position.ReadValue();
    }

    protected void SetSelected(GameObject go)
    {
        if (CurrentSelected == go)
            return;

        if (CurrentSelected != null)
        {
            if (CurrentSelected.TryGetComponent<ISelectable>(out var prev))
                prev.OnDeselected();
        }

        CurrentSelected = go;

        if (CurrentSelected != null)
        {
            if (CurrentSelected.TryGetComponent<ISelectable>(out var next))
                next.OnSelected();
        }
    }

    protected abstract void TrySelect(Vector2 screenPosition);
}
