using UnityEngine;

namespace UIFramework
{
    [ExecuteAlways]
    public class UIDebugValidator : MonoBehaviour
    {
        private void Start()
        {
            Validate();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // editor-time light validation
            if (!Application.isPlaying) return;
            Validate();
        }
#endif
        public void Validate()
        {
            if (UIScreenManager.Instance == null) Debug.LogWarning("UIScreenManager not present in scene.");
            if (ScreenMetricsProvider.Instance == null) Debug.LogWarning("ScreenMetricsProvider not present in scene.");
            if (UIFocusController.Instance == null) Debug.LogWarning("UIFocusController not present in scene.");
            // one-time checks for common pitfalls
        }
    }
}
