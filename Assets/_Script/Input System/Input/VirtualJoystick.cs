using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Input
{
    [DisallowMultipleComponent]
    public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform handle;
        [SerializeField] private float maxRadius = 80f;
        [SerializeField, Range(0f, 0.5f)] private float deadZone = 0.15f;

        private Canvas _canvas;
        private Camera _uiCamera;
        private Vector2 _direction;
        public Vector2 Direction => _direction;

        private void Awake()
        {
            if (background == null || handle == null) { Debug.LogError("[VirtualJoystick] assign background/handle"); enabled = false; return; }
            _canvas = GetComponentInParent<Canvas>();
            _uiCamera = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? _canvas.worldCamera : null;
        }

        public void OnPointerDown(PointerEventData eventData) => OnDrag(eventData);

        public void OnDrag(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, _uiCamera, out var local))
                return;
            Vector2 clamped = Vector2.ClampMagnitude(local, maxRadius);
            handle.anchoredPosition = clamped;
            Vector2 norm = clamped / maxRadius;
            _direction = (norm.magnitude < deadZone) ? Vector2.zero : norm;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            handle.anchoredPosition = Vector2.zero;
            _direction = Vector2.zero;
        }
    }
}
