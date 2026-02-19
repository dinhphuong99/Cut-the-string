using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class RopeControlAdapter : MonoBehaviour
{
    [SerializeField] private InputReader inputReader;
    [SerializeField] private SelectionController2D selectionController;

    private void Awake()
    {
        if (inputReader == null)
            inputReader = FindObjectOfType<InputReader>();

        if (selectionController == null)
            selectionController = FindObjectOfType<SelectionController2D>();
    }

    private void Update()
    {
        GameObject selected = selectionController.CurrentSelected;
        if (selected == null)
            return;

        if (!selected.TryGetComponent<RopeRetractReleaseController>(out var rope))
            return;

        var gameplay = inputReader.Actions.Gameplay;

        // Speed boost
        rope.SetSpeedMultiplier(
            gameplay.SpeedBoost.IsPressed() ? 2f : 1f
        );

        // Retract
        if (gameplay.Retract.IsPressed())
            rope.StartRetract();
        else
            rope.StopRetract();

        // Release
        if (gameplay.Release.IsPressed())
            rope.StartRelease();
        else
            rope.StopRelease();
    }
}
