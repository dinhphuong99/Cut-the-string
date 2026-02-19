using System.Collections.Generic;
using UnityEngine;

namespace UIFramework
{
    [RequireComponent(typeof(Canvas))]
    public class UILayoutController : MonoBehaviour
    {
        [System.Serializable]
        public class LayoutVariant
        {
            public string name;
            public GameObject root; // variant root (enable/disable)
            public float minAspect = 0f;
            public float maxAspect = float.MaxValue;
        }

        [SerializeField]
        private List<LayoutVariant> variants = new List<LayoutVariant>();

        private void Start()
        {
            ApplyVariant();
        }

        public void ApplyVariant()
        {
            ScreenMetricsProvider.Instance?.Refresh();
            float aspect = ScreenMetricsProvider.Instance != null ? ScreenMetricsProvider.Instance.AspectRatio :
                (float)Screen.width / Mathf.Max(1, Screen.height);

            foreach (var v in variants)
            {
                if (v.root == null) continue;
                bool inRange = aspect >= v.minAspect && aspect <= v.maxAspect;
                v.root.SetActive(inRange);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // ensure no null roots accidentally left active in edit
            if (!Application.isPlaying)
            {
                foreach (var v in variants) if (v.root != null) v.root.SetActive(true);
            }
        }
#endif
    }
}
