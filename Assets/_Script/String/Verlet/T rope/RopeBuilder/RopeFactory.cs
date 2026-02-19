using System;
using System.Collections.Generic;
using UnityEngine;

public static class RopeFactory
{
    /// <summary>
    /// Create runtime rope: creates GameObject, adds VerletRope, copies module component types from blueprint (serialized fields copied).
    /// configure: optional action to run after creating modules but before InitializeRuntime.
    /// </summary>
    public static VerletRope7 CreateRope(
        RopeProfile profile,
        List<RopeNode4> nodes,
        Transform start,
        Transform end,
        GameObject logicBlueprint,
        Action<GameObject> configure = null)
    {
        var go = new GameObject("VerletRope_Runtime");
        var rope = go.AddComponent<VerletRope7>();
        // copy profile reference
        rope.profile = profile;

        // clone modules from blueprint (only component types)
        if (logicBlueprint != null)
        {
            var comps = logicBlueprint.GetComponents<MonoBehaviour>();
            foreach (var comp in comps)
            {
                var t = comp.GetType();
                // skip if it's VerletRope7 in blueprint
                if (t == typeof(VerletRope7)) continue;
                var added = (MonoBehaviour)go.AddComponent(t);
                // copy serialized fields via Json (works for plain serializable fields and no scene refs in blueprint)
                try
                {
                    var json = UnityEngine.JsonUtility.ToJson(comp);
                    UnityEngine.JsonUtility.FromJsonOverwrite(json, added);
                }
                catch
                {
                    // ignore copy failure; configure action can fix fields
                }
            }
        }

        // allow caller to configure module references (wiring bob, anchors, etc.)
        configure?.Invoke(go);

        // finalize rope runtime
        rope.InitializeRuntime(profile, nodes, start, end);
        return rope;
    }
}