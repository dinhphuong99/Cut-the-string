namespace Game.Input
{
    // Pull model: InputHub calls UpdateIntent on registered sources.
    public interface IInputSource
    {
        void UpdateIntent(InputIntent intent);
        void OnContextChanged(InputContextKey newContext);
    }
}
