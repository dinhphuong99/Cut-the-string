using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// High-level builder to produce a ready-to-use Rope GameObject:
/// - compose an IRopeBuilder (creates node data)
/// - attach VerletRope7 and optional modules via installers
/// - call InitializeRuntime on VerletRope7
/// </summary>
public class RopeBuilder
{
    private IRopeBuilder nodeBuilder;
    private readonly List<Action<GameObject>> moduleInstallers = new();
    private RopeProfile profile;
    private Vector3 startPos;
    private Vector3 endPos;
    private Transform startTransform;
    private Transform endTransform;
    private bool pinStart = true;
    private bool pinEnd = true;

    public RopeBuilder WithProfile(RopeProfile p) { profile = p; return this; }
    public RopeBuilder UsingNodeBuilder(IRopeBuilder b) { nodeBuilder = b; return this; }

    public RopeBuilder WithEndpoints(Transform start, Transform end)
    {
        startTransform = start;
        endTransform = end;
        startPos = start != null ? start.position : Vector3.zero;
        endPos = end != null ? end.position : Vector3.zero;
        pinStart = start != null;
        pinEnd = end != null;
        return this;
    }

    public RopeBuilder WithWorldEndpoints(Vector3 start, Vector3 end)
    {
        startPos = start;
        endPos = end;
        startTransform = null;
        endTransform = null;
        return this;
    }

    public RopeBuilder WithNodeCount(int count)
    {
        if (nodeBuilder == null)
            nodeBuilder = new CompressStretchRopeBuilder(count);
        return this;
    }

    // Module installers: each is an action that receives the new GO and installs a component/config.
    public RopeBuilder AddSimulation()
    {
        moduleInstallers.Add(go => go.AddComponent<RopeSimulation>());
        return this;
    }

    public RopeBuilder AddCutter()
    {
        moduleInstallers.Add(go =>
        {
            var c = go.AddComponent<RopeCutter>();
            // optionally set default config here
        });
        return this;
    }

    public RopeBuilder AddRetractRelease()
    {
        moduleInstallers.Add(go => go.AddComponent<RopeRetractController>());
        return this;
    }

    // Generic installer if needed
    public RopeBuilder AddInstaller(Action<GameObject> installer) { moduleInstallers.Add(installer); return this; }

    /// <summary>
    /// Build a GameObject rope and return it.
    /// </summary>
    public GameObject Build(string name = "Rope_Runtime")
    {
        if (profile == null)
        {
            Debug.LogError("[RopeBuilder] Missing RopeProfile. Call WithProfile(...)");
            return null;
        }

        if (nodeBuilder == null)
        {
            Debug.Log("[RopeBuilder] No nodeBuilder specified. Using default CompressStretch(35).");
            nodeBuilder = new CompressStretchRopeBuilder(35);
        }

        var nodes = nodeBuilder.Build(profile, startPos, endPos, pinStart, pinEnd);
        if (nodes == null || nodes.Count < 2)
        {
            Debug.LogError("[RopeBuilder] builder returned invalid nodes");
            return null;
        }

        // create GO and add core
        var go = new GameObject(name);
        var verlet = go.AddComponent<VerletRope7>();

        // add optional modules (simulation, cutter, etc) before initialize if they have config copy requirements
        foreach (var inst in moduleInstallers)
            inst(go);

        // initialize runtime data (verlet will spawn render from profile)
        verlet.InitializeRuntime(profile, nodes, startTransform, endTransform);

        return go;
    }

    /// <summary>
    /// Convenience: build headless RopeData (no GameObject) -> returns nodes
    /// </summary>
    public List<RopeNode4> BuildNodes()
    {
        if (nodeBuilder == null) nodeBuilder = new CompressStretchRopeBuilder(35);
        return nodeBuilder.Build(profile, startPos, endPos, pinStart, pinEnd);
    }
}
