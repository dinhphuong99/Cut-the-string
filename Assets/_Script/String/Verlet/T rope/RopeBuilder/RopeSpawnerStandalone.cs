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

    [Header("Optional: Pendulum auto wiring")]
    [SerializeField] private PendulumPhysicsProfile pendulumProfile; // assign if you want auto pendulum
    [SerializeField] private bool autoAddPendulumModuleIfMissing = true;

    private VerletRope7 currentRope;

    [ContextMenu("Create Rope")]
    public void CreateRope()
    {
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

        IRopeBuilder builder = new CompressStretchRopeBuilder(nodeCount);

        List<RopeNode4> nodes = builder.Build(
            ropeProfile,
            startAnchor.position,
            endAnchor.position,
            pinStart: true,
            pinEnd: true
        );

        if (nodes == null || nodes.Count < 2)
        {
            Debug.LogError("[RopeSpawnerStandalone] Builder returned invalid nodes");
            return;
        }

        GameObject blueprint = CreateLogicBlueprint();

        // configure callback: will run after modules copied but BEFORE rope.InitializeRuntime (so adapter + module see config at init)
        System.Action<GameObject> configure = (runtimeGo) =>
        {
            // ensure there is a RopePendulumAdapter
            var adapter = runtimeGo.GetComponent<RopePendulumAdapter>();
            if (adapter == null)
            {
                adapter = runtimeGo.AddComponent<RopePendulumAdapter>();
            }

            // wire adapter references
            adapter.bob = endAnchor != null ? endAnchor.GetComponent<Rigidbody2D>() : null;
            adapter.anchor = startAnchor;
            adapter.pendulumProfile = pendulumProfile;

            // ensure PendulumModule exists (copied from blueprint or added now)
            var pendModule = runtimeGo.GetComponent<PendulumModule>();
            if (pendModule == null && autoAddPendulumModuleIfMissing)
            {
                runtimeGo.AddComponent<PendulumModule>();
            }
        };

        currentRope = RopeFactory.CreateRope(
            ropeProfile,
            nodes,
            startAnchor,
            endAnchor,
            blueprint,
            configure
        );

        // destroy blueprint (no scene refs kept)
        if (Application.isPlaying)
            Destroy(blueprint);
        else
            DestroyImmediate(blueprint);
    }

    [ContextMenu("Destroy Rope")]
    public void DestroyRope()
    {
        if (currentRope == null) return;
        Destroy(currentRope.gameObject);
        currentRope = null;
    }

    private GameObject CreateLogicBlueprint()
    {
        GameObject go = new GameObject("Rope_LogicBlueprint");
        go.hideFlags = HideFlags.HideAndDontSave;

        // Add module types only. Keep blueprint free of scene object references.
        go.AddComponent<RopeSimulation>(); // keep your existing module names (or rename accordingly)
        go.AddComponent<RopeCutter>();
        go.AddComponent<RopeRetractReleaseController1>();
        go.AddComponent<PendulumModule>(); // copied so runtime rope has same module type

        return go;
    }
}