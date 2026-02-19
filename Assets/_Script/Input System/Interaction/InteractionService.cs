using UnityEngine;
using Game.Selection;

namespace Game.Interaction
{
    public sealed class InteractionService
    {
        private readonly SelectionServiceBase _selection;
        private readonly float _globalCooldown;
        private float _lastTime = -999f;

        public InteractionService(SelectionServiceBase selectionService, float globalCooldownSeconds = 0.25f)
        {
            _selection = selectionService;
            _globalCooldown = globalCooldownSeconds;
        }

        public bool TryInteract(GameObject instigator)
        {
            var target = _selection?.Current;
            if (target == null) return false;
            if (!(target is IInteractable interactable)) return false;

            float now = Time.time;
            if (now - _lastTime < _globalCooldown) return false;

            var ctx = new InteractionContext(instigator, target);
            if (!interactable.CanInteract(ctx)) return false;

            float sq = (target.Transform.position - ctx.Origin).sqrMagnitude;
            if (sq > interactable.InteractionRange * interactable.InteractionRange) return false;

            interactable.Interact(ctx);
            _lastTime = now;
            return true;
        }
    }
}
