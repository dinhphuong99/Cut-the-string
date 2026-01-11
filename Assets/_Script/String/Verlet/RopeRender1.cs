using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class RopeRender1 : MonoBehaviour
{
    [SerializeField] private LineRenderer line;

    private Material baseMaterial;
    private Material blinkMaterial;

    private VerletRope7 rope;

    private bool isInitialized = false;
    private bool isBlinking = false;

    private float blinkTimer = 0f;
    private float blinkSpeed = 6f;

    void Awake()
    {
        if (line == null)
            line = GetComponent<LineRenderer>();
    }

    // Controller gọi khi spawn rope
    public void Initialize(VerletRope7 ropeRef, Material sharedMaterial)
    {
        rope = ropeRef;
        baseMaterial = sharedMaterial;

        // instance riêng để đổi color runtime mà không ảnh hưởng sharedMaterial
        // nếu bạn không cần đổi màu ở runtime thì có thể dùng sharedMaterial
        line.material = new Material(baseMaterial);

        // default width
        line.startWidth = 0.05f;
        line.endWidth = 0.05f;

        isInitialized = true;
    }

    public void SetBlink(bool active, Material blinkMat = null)
    {
        if (active == isBlinking) return;
        isBlinking = active;

        if (active && blinkMat != null)
        {
            blinkMaterial = blinkMat;
        }
    }

    public void SetWidth(float width)
    {
        line.startWidth = width;
        line.endWidth = width;
    }

    public void SetColor(Color c)
    {
        // dùng instance riêng
        if (line.material != null)
            line.material.color = c;
    }

    void Update()
    {
        if (!isInitialized || rope == null) return;

        UpdateLinePositions();

        if (isBlinking)
            UpdateBlink();
    }

    private void UpdateLinePositions()
    {
        List<RopeNode4> nodes = rope.nodes;
        if (nodes == null || nodes.Count == 0) return;

        line.positionCount = nodes.Count;

        for (int i = 0; i < nodes.Count; i++)
        {
            line.SetPosition(i, nodes[i].position);
        }
    }

    private void UpdateBlink()
    {
        if (blinkMaterial == null || baseMaterial == null)
        {
            // fallback: chỉ thay color
            blinkTimer += Time.deltaTime * blinkSpeed;
            float t = (Mathf.Sin(blinkTimer) + 1f) * 0.5f;

            Color c = line.material.color;
            c.a = Mathf.Lerp(1f, 0f, t);
            line.material.color = c;
            return;
        }

        blinkTimer += Time.deltaTime * blinkSpeed;
        float s = Mathf.Sin(blinkTimer);

        if (line != null)
        {
            line.material = (s > 0f) ? blinkMaterial : baseMaterial;
        }
    }
}
