using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class InputReader : MonoBehaviour
{
    public GameInputActions Actions { get; private set; }

    private void Awake()
    {
        Actions = new GameInputActions();
    }

    private void OnEnable()
    {
        Actions.Enable();
    }

    private void OnDisable()
    {
        Actions.Disable();
    }
}
