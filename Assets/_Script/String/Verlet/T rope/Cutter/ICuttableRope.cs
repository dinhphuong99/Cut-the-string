public interface ICuttableRope
{
    bool CanBeCut { get; }
    float GetStretch { get; }      // 0..1 (current / max)
    int RecommendedCutIndex { get; // -1 nếu không hợp lệ
    }

    bool CutAt(int nodeIndex);
}