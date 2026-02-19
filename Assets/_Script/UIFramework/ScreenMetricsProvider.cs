using UnityEngine;

namespace UIFramework
{
    [DefaultExecutionOrder(-100)]
    public class ScreenMetricsProvider : MonoBehaviour
    {
        public static ScreenMetricsProvider Instance { get; private set; }

        public Rect SafeArea { get; private set; }
        public float DPI { get; private set; }
        public Vector2Int Resolution { get; private set; }
        public float AspectRatio { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            Refresh();
        }

        private void OnRectTransformDimensionsChange()
        {
            // In editor/play, if resolution changes
            Refresh();
        }

        public void Refresh()
        {
            Resolution = new Vector2Int(Screen.width, Screen.height);
            SafeArea = Screen.safeArea;
            DPI = Screen.dpi;
            AspectRatio = (float)Screen.width / Mathf.Max(1, Screen.height);
        }

        public bool IsWide(float threshold = 1.8f) => AspectRatio >= threshold;
        public bool IsNarrow(float threshold = 0.9f) => AspectRatio <= threshold;
    }
}
