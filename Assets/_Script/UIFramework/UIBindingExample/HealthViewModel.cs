using UnityEngine;
using UnityEngine.UI;

namespace UIFramework
{
    [CreateAssetMenu(menuName = "UI/HealthViewModel")]
    public class HealthViewModel : ScriptableObject
    {
        public float current = 1f;
        public float max = 1f;

        public delegate void OnHealthChanged(float cur, float max);
        public event OnHealthChanged HealthChanged;

        public void SetHealth(float cur, float m)
        {
            current = cur;
            max = m;
            HealthChanged?.Invoke(current, max);
        }
    }

    // example HUD screen listening to viewmodel
    public class HUDScreen : UIScreen
    {
        [Header("Bindings")]
        public HealthViewModel healthVM;
        public Image healthFill;

        protected override void OnAfterEnter()
        {
            base.OnAfterEnter();
            if (healthVM != null) healthVM.HealthChanged += OnHealthChanged;
            // initial update
            if (healthVM != null && healthFill != null)
            {
                healthFill.fillAmount = healthVM.max <= 0 ? 0f : healthVM.current / healthVM.max;
            }
        }

        protected override void OnBeforeExit()
        {
            base.OnBeforeExit();
            if (healthVM != null) healthVM.HealthChanged -= OnHealthChanged;
        }

        private void OnHealthChanged(float c, float m)
        {
            if (healthFill != null)
            {
                healthFill.fillAmount = m <= 0 ? 0f : c / m;
            }
        }
    }
}
