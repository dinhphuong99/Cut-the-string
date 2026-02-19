public interface IRopeModule
{
    /// <summary>
    /// Called once when rope runtime is ready. Module may query provider.
    /// </summary>
    void Initialize(IRopeDataProvider provider);
}
