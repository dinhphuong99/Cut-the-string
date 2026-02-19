using System.Collections.Generic;
using UnityEngine;

namespace Game.Selection
{
    public class SelectionHistory : MonoBehaviour
    {
        public static SelectionHistory Instance { get; private set; }
        Dictionary<ISelectable, float> lastSelectedTime = new Dictionary<ISelectable, float>();

        void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            Instance = this;
        }

        public void RecordSelection(ISelectable s)
        {
            if (s == null) return;
            lastSelectedTime[s] = Time.time;
        }

        public bool WasSelectedWithin(ISelectable s, float seconds)
        {
            if (s == null) return false;
            if (!lastSelectedTime.TryGetValue(s, out var t)) return false;
            return (Time.time - t) <= seconds;
        }
    }
}