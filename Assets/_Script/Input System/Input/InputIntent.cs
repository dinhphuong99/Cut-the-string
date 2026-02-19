using System.Collections.Generic;

namespace Game.Input
{
    // Data-driven intent keyed by InputActionKey.
    public sealed class InputIntent
    {
        private readonly Dictionary<InputActionKey, InputActionValue> _map = new(16);

        public void Set(InputActionKey key, InputActionValue value)
        {
            if (key == null) return;
            _map[key] = value;
        }

        public bool TryGet(InputActionKey key, out InputActionValue value)
        {
            if (key == null) { value = default; return false; }
            return _map.TryGetValue(key, out value);
        }

        public void Clear() => _map.Clear();
    }
}
