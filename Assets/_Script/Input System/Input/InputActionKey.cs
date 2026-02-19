using UnityEngine;

namespace Game.Input
{
    [CreateAssetMenu(menuName = "Game/Input/ActionKey", fileName = "InputActionKey")]
    public sealed class InputActionKey : ScriptableObject
    {
        [SerializeField] private string id;
        public string Id => id;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id))
                id = name;
        }
#endif
    }
}
