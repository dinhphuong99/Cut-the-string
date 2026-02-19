using UnityEngine;

public class RopeCutter : MonoBehaviour, IRopeModule
{
    [SerializeField] private MonoBehaviour ropeSource; // optional manual override
    [SerializeField] private float stretchThreshold = 0.94f;

    private ICuttableRope cuttable;
    private bool initialized = false;
    private bool hasCut = false;

    public void Initialize(IRopeDataProvider rp)
    {
        if (initialized) return;

        // try override source first
        if (ropeSource != null && ropeSource is ICuttableRope r)
            cuttable = r;
        else if (rp is ICuttableRope rc)
            cuttable = rc;

        if (cuttable == null)
        {
            Debug.LogWarning("[RopeCutter] ICuttableRope not available. Disabling.", this);
            enabled = false;
            return;
        }

        initialized = true;
    }

    private void FixedUpdate()
    {
        if (!initialized || !cuttable.CanBeCut || hasCut) return;
        float stretch = cuttable.GetStretch;
        if (stretch >= stretchThreshold)
        {
            int idx = cuttable.RecommendedCutIndex;
            if (idx >= 0)
            {
                hasCut = cuttable.CutAt(idx);
            }
        }
    }

    public bool CutManually(int index)
    {
        if (!initialized || hasCut || !cuttable.CanBeCut) return false;
        hasCut = cuttable.CutAt(index);
        return hasCut;
    }
}