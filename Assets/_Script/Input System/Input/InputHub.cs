using System.Collections.Generic;
using UnityEngine;

namespace Game.Input
{
    [DisallowMultipleComponent]
    public sealed class InputHub : MonoBehaviour
    {
        private readonly List<IInputSource> _sources = new();
        private readonly InputIntent _intent = new();

        public InputIntent Current => _intent;

        private void Awake()
        {
            InputRegistry.Initialize();
        }

        public void RegisterSource(IInputSource src)
        {
            if (src == null) return;
            if (!_sources.Contains(src)) _sources.Add(src);
        }

        public void UnregisterSource(IInputSource src)
        {
            if (src == null) return;
            _sources.Remove(src);
        }

        public void SetContext(InputContextKey context)
        {
            for (int i = 0; i < _sources.Count; i++)
                _sources[i].OnContextChanged(context);
        }

        private void Update()
        {
            _intent.Clear();
            for (int i = 0; i < _sources.Count; i++)
                _sources[i].UpdateIntent(_intent);
        }
    }
}
