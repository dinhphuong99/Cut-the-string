using UnityEngine;

namespace Game.Input
{
    [DisallowMultipleComponent]
    public sealed class BootstrapRegister : MonoBehaviour
    {
        [SerializeField] private InputHub hub;

        private void Awake()
        {
            InputRegistry.Initialize();
            if (hub == null) hub = FindObjectOfType<InputHub>();
            if (hub == null) { Debug.LogError("[BootstrapRegister] InputHub not found"); enabled = false; return; }

            var monos = GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < monos.Length; i++)
                if (monos[i] is IInputSource s) hub.RegisterSource(s);
        }
    }
}
