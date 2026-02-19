using System.Collections;
using UnityEngine;

namespace UIFramework
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIScreen : MonoBehaviour
    {
        [Tooltip("Unique ID (use same ID when registering with ScreenManager)")]
        public string screenId;

        [Tooltip("If true this screen blocks screens below (modal)")]
        public bool isModal = false;

        [Tooltip("If true UI will block gameplay input while active")]
        public bool requiresExclusiveInput = true;

        protected CanvasGroup canvasGroup;
        public bool IsShowing { get; private set; } = false;

        protected virtual void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            // default hidden
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        // Called by ScreenManager when pushing this screen (first time).
        public virtual IEnumerator Enter()
        {
            gameObject.SetActive(true);
            IsShowing = true;
            yield return UITransitionController.FadeIn(canvasGroup, 0.18f);
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            OnAfterEnter();
        }

        // Called by ScreenManager when popping (final exit).
        public virtual IEnumerator Exit()
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            OnBeforeExit();
            yield return UITransitionController.FadeOut(canvasGroup, 0.12f);
            IsShowing = false;
            gameObject.SetActive(false);
        }

        // Called when another screen overlays this (modal above)
        public virtual void Pause()
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            OnPause();
        }

        // Called when overlaying screen removed and this resumes
        public virtual void Resume()
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            OnResume();
        }

        protected virtual void OnBeforeExit() { }
        protected virtual void OnAfterEnter() { }
        protected virtual void OnPause() { }
        protected virtual void OnResume() { }

        // safety: allow screens to veto close (e.g. unsaved changes)
        public virtual bool CanExit() => true;
    }
}
