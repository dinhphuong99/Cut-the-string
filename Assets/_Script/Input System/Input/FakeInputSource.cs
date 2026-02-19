using System.Collections.Generic;
using UnityEngine;

namespace Game.Input
{
    [DisallowMultipleComponent]
    public sealed class FakeInputSource : MonoBehaviour, IInputSource
    {
        private readonly Dictionary<InputActionKey, InputActionValue> _state = new();

        public void SetButton(string actionId, bool value)
        {
            var k = InputRegistry.GetActionKey(actionId);
            if (k == null)
            {
                var tmp = ScriptableObject.CreateInstance<InputActionKey>();
#if UNITY_EDITOR
                tmp.hideFlags = HideFlags.DontSave;
#endif
                var f = typeof(InputActionKey).GetField("id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                f?.SetValue(tmp, actionId);
                InputRegistry.RegisterActionKey(tmp);
                k = tmp;
            }
            _state[k] = InputActionValue.FromButton(value);
        }

        public void SetVector(string actionId, Vector2 v)
        {
            var k = InputRegistry.GetActionKey(actionId);
            if (k == null)
            {
                var tmp = ScriptableObject.CreateInstance<InputActionKey>();
#if UNITY_EDITOR
                tmp.hideFlags = HideFlags.DontSave;
#endif
                var f = typeof(InputActionKey).GetField("id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                f?.SetValue(tmp, actionId);
                InputRegistry.RegisterActionKey(tmp);
                k = tmp;
            }
            _state[k] = InputActionValue.FromVector(v);
        }

        public void ClearAll() => _state.Clear();

        public void UpdateIntent(InputIntent intent)
        {
            foreach (var kv in _state) intent.Set(kv.Key, kv.Value);
        }

        public void OnContextChanged(InputContextKey newContext) { /* optional */ }
    }
}
