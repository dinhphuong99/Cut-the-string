// Assets/Scripts/AnimationSystem/Runtime/AnimationTestDriver.cs
using UnityEngine;

[RequireComponent(typeof(AnimationCoordinator))]
public class AnimationTestDriver : MonoBehaviour
{
    public AnimationCoordinator coordinator;
    public string intentIdle = "Idle";
    public string intentMove = "Move";
    public string intentAttack = "Attack";
    public string variant = "Light";
    public float baseAttackDuration = 1.0f;

    void Reset() { coordinator = GetComponent<AnimationCoordinator>(); }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) coordinator.RequestIntent(new AnimationIntent(intentIdle));
        if (Input.GetKeyDown(KeyCode.Alpha2)) coordinator.RequestIntent(new AnimationIntent(intentMove));
        if (Input.GetKeyDown(KeyCode.Space)) coordinator.RequestIntent(new AnimationIntent(intentAttack, variant, baseAttackDuration));
        if (Input.GetKeyDown(KeyCode.Alpha9)) coordinator.RequestIntent(new AnimationIntent(intentAttack, variant, baseAttackDuration, 1.5f)); // speed up
        if (Input.GetKeyDown(KeyCode.Alpha0)) coordinator.CancelCurrent();
    }
}