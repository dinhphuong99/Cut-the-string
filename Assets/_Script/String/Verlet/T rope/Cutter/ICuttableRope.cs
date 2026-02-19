public interface ICuttableRope
{
    bool CanBeCut { get; }
    float GetStretch { get; } // 0..inf
    int RecommendedCutIndex { get; }
    bool CutAt(int index);
}