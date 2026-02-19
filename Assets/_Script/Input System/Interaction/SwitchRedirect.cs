using UnityEngine;
using Game.Interaction;

public class SwitchRedirect : MonoBehaviour, IInteractionRedirect
{
    [SerializeField] private GameObject targetObject;

    public GameObject GetInteractionTarget()
    {
        return targetObject;
    }
}
