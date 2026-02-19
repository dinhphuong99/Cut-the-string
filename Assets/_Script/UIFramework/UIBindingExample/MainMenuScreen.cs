using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace UIFramework
{
    public class MainMenuScreen : UIScreen
    {
        public Button startButton;
        public Button settingsButton;
        public Button quitButton;

        protected override void OnAfterEnter()
        {
            base.OnAfterEnter();
            if (startButton != null) startButton.onClick.AddListener(OnStart);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettings);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuit);

            // set default focus
            UIFocusController.Instance?.SetSelected(startButton?.gameObject);
        }

        protected override void OnBeforeExit()
        {
            base.OnBeforeExit();
            if (startButton != null) startButton.onClick.RemoveListener(OnStart);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettings);
            if (quitButton != null) quitButton.onClick.RemoveListener(OnQuit);
        }

        private void OnStart()
        {
            // example: push HUD or gameplay screen id
            UIScreenManager.Instance.Push("HUD");
        }

        private void OnSettings()
        {
            UIScreenManager.Instance.Push("Settings");
        }

        private void OnQuit()
        {
            Application.Quit();
        }
    }
}
