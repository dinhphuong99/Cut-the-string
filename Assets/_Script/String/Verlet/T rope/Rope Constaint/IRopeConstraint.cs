public interface IRopeConstraint
{
    bool IsActive { get; }
    void Apply(ref RopeNode4 node);
}