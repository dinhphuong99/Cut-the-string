using UnityEngine;

public class RopeCutter : MonoBehaviour, IRopeModule
{
    [SerializeField] private MonoBehaviour ropeSource;
    [SerializeField] private float stretchThreshold = 0.94f;
    private IRopeDataProvider data;
    private bool initialized = false;

    private ICuttableRope rope;
    private bool hasCut;

    public void Initialize(IRopeDataProvider rp)
    {
        if (initialized) return;

        data = rp;

        rope = rp as ICuttableRope;
        if (rope == null)
        {
            Debug.LogWarning("[RopeCutter] ICuttableRope not supported", this);
            enabled = false;
            return;
        }

        initialized = true;
    }

    public void CopyConfigFrom(RopeCutter other)
    {
        stretchThreshold = other.stretchThreshold;
        // KHÔNG đụng vào ropeSource ở runtime
    }

    private void FixedUpdate()
    {
        if (!initialized)
            return;

        if (!data.IsReady)
            return;

        TryAutoCutRope();
    }

    private ICuttableRope ResolveRope()
    {
        if (ropeSource != null)
        {
            var r = ropeSource as ICuttableRope;
            if (r != null)
                return r;

            Debug.LogError(
                "[RopeCutter] ropeSource does not implement ICuttableRope",
                this
            );
            return null;
        }

        return GetComponent<ICuttableRope>();
    }

    private void TryAutoCutRope()
    {
        if (!initialized) return;

        if (hasCut || !rope.CanBeCut)
            return;

        if (rope.GetStretch < stretchThreshold)
            return;

        int cutIndex = rope.RecommendedCutIndex;
        if (cutIndex < 0)
            return;

        hasCut = true;
        rope.CutAt(cutIndex);
    }

    public bool CutManually(int cutIndex)
    {
        if (hasCut || !rope.CanBeCut)
            return false;

        hasCut = rope.CutAt(cutIndex);
        return hasCut;
    }
}
