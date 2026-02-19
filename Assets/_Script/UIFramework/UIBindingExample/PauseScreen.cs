using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace UIFramework
{
    public class PauseScreen : UIScreen
    {
        public Button resumeButton;
        public Button mainMenuButton;

        protected override void OnAfterEnter()
        {
            base.OnAfterEnter();
            Time.timeScale = 0f; // block gameplay
            if (resumeButton != null) resumeButton.onClick.AddListener(OnResume);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenu);
            UIFocusController.Instance?.SetSelected(resumeButton?.gameObject);
        }

        protected override void OnBeforeExit()
        {
            base.OnBeforeExit();
            Time.timeScale = 1f;
            if (resumeButton != null) resumeButton.onClick.RemoveListener(OnResume);
            if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(OnMainMenu);
        }

        private void OnResume() => UIScreenManager.Instance.Pop();
        private void OnMainMenu() => UIScreenManager.Instance.Replace("MainMenu");
    }
}
