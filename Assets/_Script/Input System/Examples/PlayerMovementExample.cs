using UnityEngine;
using Game.Input;

namespace Game.Examples
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerMovementExample : MonoBehaviour
    {
        [SerializeField] private InputHub hub;
        [SerializeField] private string moveActionId = "Move";
        [SerializeField] private float speed = 5f;

        private Rigidbody _rb;
        private InputActionKey _moveKey;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            if (hub == null) hub = FindObjectOfType<InputHub>();
            InputRegistry.Initialize();
            _moveKey = InputRegistry.GetActionKey(moveActionId);
        }

        private void FixedUpdate()
        {
            if (hub == null || _moveKey == null) return;
            if (hub.Current.TryGet(_moveKey, out var v))
            {
                Vector2 m = v.Vector;
                Vector3 vel = new Vector3(m.x, 0f, m.y) * speed;
                vel.y = _rb.linearVelocity.y;
                _rb.linearVelocity = vel;
            }
        }
    }
}
