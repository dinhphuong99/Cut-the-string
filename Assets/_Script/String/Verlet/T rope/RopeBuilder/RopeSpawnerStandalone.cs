using System.Collections.Generic;
using UnityEngine;

public class RopeSpawnerStandalone : MonoBehaviour
{
    [Header("Rope Data")]
    [SerializeField] private RopeProfile ropeProfile;

    [Header("Anchors")]
    [SerializeField] private Transform startAnchor;
    [SerializeField] private Transform endAnchor;

    [Header("Builder")]
    [SerializeField] private int nodeCount = 30;

    [SerializeField] private RopeBuilderAsset ropeBuilder;


    [Header("Optional: Pendulum auto wiring")]
    [SerializeField] private PendulumBehaviorProfile pendulumBehavior;
    [SerializeField] private bool autoAddPendulumModuleIfMissing = true;

    private VerletRope7 currentRope;

    [ContextMenu("Create Rope")]
    public void CreateRope()
    {
        if (ropeBuilder == null)
        {
            Debug.LogError("[RopeSpawnerStandalone] Missing RopeBuilderAsset");
            return;
        }

        if (currentRope != null)
        {
            Debug.LogWarning("[RopeSpawnerStandalone] Rope already exists");
            return;
        }

        if (ropeProfile == null || startAnchor == null || endAnchor == null)
        {
            Debug.LogError("[RopeSpawnerStandalone] Missing setup");
            return;
        }

        IRopeBuilder builder = ropeBuilder.CreateBuilder(nodeCount);

        var nodes = builder.Build(ropeProfile, startAnchor.position, endAnchor.position, true, true);
        if (nodes == null || nodes.Count < 2)
        {
            Debug.LogError("[RopeSpawnerStandalone] Builder returned invalid nodes");
            return;
        }

        var blueprint = CreateLogicBlueprint();

        System.Action<GameObject> configure = (runtimeGo) =>
        {
            // ---- RopeControlAdapter ----
            if (runtimeGo.GetComponent<RopeControlAdapter>() == null)
            {
                runtimeGo.AddComponent<RopeControlAdapter>();
            }

            // wire pendulum adapter if present
            var adapter = runtimeGo.GetComponent<RopePendulumAdapter>();
            if (adapter == null)
                adapter = runtimeGo.AddComponent<RopePendulumAdapter>();

            adapter.anchor = startAnchor;
            adapter.bob = endAnchor != null ? endAnchor.GetComponent<Rigidbody2D>() : null;
            adapter.pendulumBehavior = pendulumBehavior;

            if (runtimeGo.GetComponent<PendulumModule>() == null && autoAddPendulumModuleIfMissing)
                runtimeGo.AddComponent<PendulumModule>();
        };

        currentRope = RopeFactory.CreateRope(ropeProfile, nodes, startAnchor, endAnchor, blueprint, configure);

        if (Application.isPlaying) Destroy(blueprint);
        else DestroyImmediate(blueprint);
    }

    private GameObject CreateLogicBlueprint()
    {
        GameObject go = new GameObject("Rope_LogicBlueprint");
        go.hideFlags = HideFlags.HideAndDontSave;

        go.AddComponent<RopeSimulation>();
        go.AddComponent<RopeCutter>();
        go.AddComponent<RopeRetractReleaseController>();
        go.AddComponent<PendulumModule>();

        return go;
    }
}