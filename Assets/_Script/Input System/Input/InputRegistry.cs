using System.Collections.Generic;
using UnityEngine;

namespace Game.Input
{
    // Lightweight runtime registry. Loads InputActionKey/InputContextKey from Resources folders.
    public static class InputRegistry
    {
        private static readonly Dictionary<string, InputActionKey> _actions = new();
        private static readonly Dictionary<string, InputContextKey> _contexts = new();
        private static bool _initialized = false;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            var keys = Resources.LoadAll<InputActionKey>("InputKeys");
            foreach (var k in keys)
                if (k != null && !string.IsNullOrEmpty(k.Id))
                    _actions[k.Id] = k;

            var contexts = Resources.LoadAll<InputContextKey>("InputContexts");
            foreach (var c in contexts)
                if (c != null && !string.IsNullOrEmpty(c.Id))
                    _contexts[c.Id] = c;
        }

        public static void RegisterActionKey(InputActionKey key)
        {
            if (key == null || string.IsNullOrEmpty(key.Id)) return;
            _initialized = true;
            _actions[key.Id] = key;
        }

        public static void RegisterContextKey(InputContextKey key)
        {
            if (key == null || string.IsNullOrEmpty(key.Id)) return;
            _initialized = true;
            _contexts[key.Id] = key;
        }

        public static InputActionKey GetActionKey(string id)
        {
            if (!_initialized) Initialize();
            if (string.IsNullOrEmpty(id)) return null;
            _actions.TryGetValue(id, out var k);
            return k;
        }

        public static InputContextKey GetContextKey(string id)
        {
            if (!_initialized) Initialize();
            if (string.IsNullOrEmpty(id)) return null;
            _contexts.TryGetValue(id, out var k);
            return k;
        }
    }
}
