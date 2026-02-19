using Game.Selection;

namespace Game.Interaction
{
    public interface IInteractable
    {
        float InteractionRange { get; }
        bool CanInteract(Game.Interaction.InteractionContext context);
        void Interact(Game.Interaction.InteractionContext context);
    }
}
