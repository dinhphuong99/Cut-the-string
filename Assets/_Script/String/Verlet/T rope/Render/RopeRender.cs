using UnityEngine;
using System;

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class RopeRender : MonoBehaviour
{
    private IRopeDataProvider rope;
    private LineRenderer line;

    [Header("Material")]
    [SerializeField] private Material defaultLineMaterial;
    [SerializeField] private bool instantiateMaterialPerRope = false;

    [Header("Width")]
    [SerializeField, Min(0.001f)]
    private float ropeWidth = 1f;

    [Header("Blink (Runtime Only)")]
    [SerializeField] private bool enableBlink = true;
    [SerializeField] private float blinkSpeed = 3f;
    [SerializeField] private Color blinkColorA = Color.red;
    [SerializeField] private Color blinkColorB = Color.white;

    private bool isBound;
    private bool isInitialized;

    private Color originalColor = Color.cyan;
    private bool cachedOriginalColor;
    private float blinkTimer;

    #region Unity Lifecycle

    private void Awake()
    {
        EnsureLineRenderer();
    }

    private void OnEnable()
    {
        EnsureLineRenderer();
        ApplyStaticLineSettings();
    }

    private void OnValidate()
    {
        EnsureLineRenderer();
        ApplyStaticLineSettings();
    }

    private void OnDestroy()
    {
        if (rope != null)
            rope.OnNodesReady -= HandleNodesReady;
    }

    #endregion

    #region Binding

    public void Bind(IRopeDataProvider targetRope)
    {
        if (targetRope == null)
        {
            Debug.LogError("[RopeRender] Bind failed: targetRope is null", this);
            return;
        }

        if (rope != null)
            rope.OnNodesReady -= HandleNodesReady;

        rope = targetRope;
        rope.OnNodesReady += HandleNodesReady;

        isBound = true;

        // Nếu rope đã sẵn sàng trước khi bind
        if (rope.IsReady)
            HandleNodesReady();
    }

    private void HandleNodesReady()
    {
        if (!isBound || rope == null)
            return;

        if (!HasValidRopeData())
        {
            Debug.LogWarning("[RopeRender] Rope data invalid on NodesReady", this);
            return;
        }

        EnsureLineRenderer();
        ApplyStaticLineSettings();
        ResizeLine();

        CacheOriginalColorIfNeeded();
        isInitialized = true;
    }

    #endregion

    #region Update Loop

    private void LateUpdate()
    {
        if (!isInitialized || rope == null)
            return;

        if (!HasValidRopeData())
            return;

        UpdateLinePositions();

        if (Application.isPlaying && enableBlink && rope.ShouldBlink)
            UpdateBlink();
        else
            ResetColor();
    }

    #endregion

    #region LineRenderer Setup

    private void EnsureLineRenderer()
    {
        if (line == null)
            line = GetComponent<LineRenderer>();

        if (line == null)
        {
            Debug.LogError("[RopeRender] LineRenderer missing", this);
            return;
        }
    }

    private void ApplyStaticLineSettings()
    {
        if (line == null)
            return;

        line.useWorldSpace = true;
        line.loop = false;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.widthMultiplier = Mathf.Max(0.0001f, ropeWidth);

        if (defaultLineMaterial != null)
        {
            if (instantiateMaterialPerRope && Application.isPlaying)
            {
                if (line.material == null || line.material == defaultLineMaterial)
                {
                    Material inst = new Material(defaultLineMaterial);
                    CopyColorPropertyIfExists(defaultLineMaterial, inst);
                    inst.renderQueue = defaultLineMaterial.renderQueue;
                    line.material = inst;
                }
            }
            else
            {
                line.sharedMaterial = defaultLineMaterial;
            }
        }
    }

    private void ResizeLine()
    {
        int count = Mathf.Max(1, rope.NodeCount);
        line.positionCount = count;
    }

    #endregion

    #region Geometry Update

    private void UpdateLinePositions()
    {
        int count = rope.NodeCount;
        line.positionCount = count;

        for (int i = 0; i < count; i++)
            line.SetPosition(i, rope.NodesPositions[i]);
    }

    private bool HasValidRopeData()
    {
        return rope != null
            && rope.NodesPositions != null
            && rope.NodeCount > 1
            && rope.NodesPositions.Count == rope.NodeCount;

    }

    #endregion

    #region Blink

    private void CacheOriginalColorIfNeeded()
    {
        if (cachedOriginalColor || line == null)
            return;

        originalColor = line.startColor;
        cachedOriginalColor = true;
    }

    private void UpdateBlink()
    {
        blinkTimer += Time.deltaTime;
        float u = (Mathf.Sin(blinkTimer * blinkSpeed) + 1f) * 0.5f;
        Color c = Color.Lerp(blinkColorA, blinkColorB, u);
        line.startColor = c;
        line.endColor = c;
    }

    private void ResetColor()
    {
        if (!cachedOriginalColor || line == null)
            return;

        line.startColor = originalColor;
        line.endColor = originalColor;
    }

    #endregion

    #region Utility

    private void CopyColorPropertyIfExists(Material src, Material dst)
    {
        if (src.HasProperty("_Color"))
            dst.SetColor("_Color", src.GetColor("_Color"));
        else if (src.HasProperty("_BaseColor"))
            dst.SetColor("_BaseColor", src.GetColor("_BaseColor"));
        else if (src.HasProperty("_TintColor"))
            dst.SetColor("_TintColor", src.GetColor("_TintColor"));
    }

    #endregion
}