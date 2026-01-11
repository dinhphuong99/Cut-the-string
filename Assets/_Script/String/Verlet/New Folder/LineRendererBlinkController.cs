using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LineRendererBlinkController : MonoBehaviour
{
    [Header("Blink Settings")]
    public bool isBlinking = false;
    public Color blinkColorA = Color.white;
    public Color blinkColorB = Color.red;
    [Range(0.1f, 20f)] public float blinkSpeed = 1f;

    [Header("Base Settings")]
    public Color defaultColor = Color.magenta;

    private LineRenderer line;
    private MaterialPropertyBlock mpb;
    private static readonly int ColorID = Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        mpb = new MaterialPropertyBlock();
    }

    private void Update()
    {
        if (!line) return;
        UpdateBlinkColor();
    }

    void UpdateBlinkColor()
    {
        if (!isBlinking)
        {
            mpb.SetColor(ColorID, defaultColor);
        }
        else
        {
            float t = (Mathf.Sin(Time.time * blinkSpeed) * 0.5f + 0.5f);
            Color c = Color.Lerp(blinkColorA, blinkColorB, t);
            mpb.SetColor(ColorID, c);
        }
        line.SetPropertyBlock(mpb);
    }

    // Dùng khi muốn sao chép toàn bộ thiết lập từ LineRenderer khác
    public void CopyLineRendererSettings(LineRenderer src)
    {
        if (!src || !line) return;

        // --- clone material ---
        if (src.sharedMaterial != null)
        {
            line.material = new Material(src.sharedMaterial);

            // Sao chép màu từ shader nếu có
            if (src.material.HasProperty("_Color"))
                line.material.color = src.material.color;
            else if (src.material.HasProperty("_BaseColor"))
                line.material.SetColor("_BaseColor", src.material.GetColor("_BaseColor"));
            else if (src.material.HasProperty("_TintColor"))
                line.material.SetColor("_TintColor", src.material.GetColor("_TintColor"));
        }

        // --- clone gradient ---
        Gradient srcGrad = src.colorGradient;
        if (srcGrad != null)
        {
            GradientColorKey[] colorKeys = new GradientColorKey[srcGrad.colorKeys.Length];
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[srcGrad.alphaKeys.Length];
            System.Array.Copy(srcGrad.colorKeys, colorKeys, srcGrad.colorKeys.Length);
            System.Array.Copy(srcGrad.alphaKeys, alphaKeys, srcGrad.alphaKeys.Length);
            Gradient cloneGrad = new Gradient();
            cloneGrad.SetKeys(colorKeys, alphaKeys);
            line.colorGradient = cloneGrad;
        }

        // --- copy width & color ---
        line.widthMultiplier = src.widthMultiplier;
        line.startWidth = src.startWidth;
        line.endWidth = src.endWidth;
        line.startColor = src.startColor;
        line.endColor = src.endColor;

        // --- copy geometry / rendering ---
        line.textureMode = src.textureMode;
        line.numCornerVertices = src.numCornerVertices;
        line.numCapVertices = src.numCapVertices;
        line.alignment = src.alignment;
        line.shadowCastingMode = src.shadowCastingMode;
        line.receiveShadows = src.receiveShadows;
        line.sortingLayerID = src.sortingLayerID;
        line.sortingOrder = src.sortingOrder;
        line.useWorldSpace = src.useWorldSpace;
        line.loop = src.loop;

        // --- copy renderQueue ---
        if (src.material != null)
            line.material.renderQueue = src.material.renderQueue;

        // --- copy positions ---
        line.positionCount = src.positionCount;
        Vector3[] temp = new Vector3[src.positionCount];
        src.GetPositions(temp);
        line.SetPositions(temp);
    }

    // Cho phép đổi trạng thái nhấp nháy runtime
    public void SetBlink(bool active, Color? colorA = null, Color? colorB = null, float speed = -1f)
    {
        isBlinking = active;
        if (colorA.HasValue) blinkColorA = colorA.Value;
        if (colorB.HasValue) blinkColorB = colorB.Value;
        if (speed > 0) blinkSpeed = speed;
    }
}