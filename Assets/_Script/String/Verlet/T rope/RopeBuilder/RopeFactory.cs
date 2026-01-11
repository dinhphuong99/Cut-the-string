using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// RopeFactory: creates a runtime VerletRope7 instance and optionally copies module components
/// from a logic blueprint GameObject. If a copied module exposes a method named
/// "CopyConfigFrom" (instance, public or non-public), that method will be invoked with
/// the source module as parameter to allow safe config copy. Otherwise a shallow copy of
/// simple serializable fields is attempted.
/// 
/// Usage:
///   var rope = RopeFactory.CreateRope(profile, nodes, start, end, blueprint, runtimeGo =>
///   {
///       // wire adapters / refs on runtimeGo here before rope.InitializeRuntime(...) is called
///   });
/// </summary>
public static class RopeFactory
{
    /// <summary>
    /// Create a runtime rope GameObject with VerletRope7 core.
    /// If logicBlueprint provided, its MonoBehaviour modules (except VerletRope7) are copied onto the runtime GO
    /// before initialization. configureAfterCopy runs after copying modules but before Rope.InitializeRuntime.
    /// </summary>
    /// <param name="profile">RopeProfile used for runtime.</param>
    /// <param name="nodes">Node list defining rope geometry.</param>
    /// <param name="start">Start anchor transform.</param>
    /// <param name="end">End anchor transform.</param>
    /// <param name="logicBlueprint">Optional blueprint GameObject containing module components to copy.</param>
    /// <param name="configureAfterCopy">Optional callback to configure runtime GO (wire adapters) before initialization.</param>
    /// <returns>Created VerletRope7 instance, or null on error.</returns>
    public static VerletRope7 CreateRope(
        RopeProfile profile,
        List<RopeNode4> nodes,
        Transform start,
        Transform end,
        GameObject logicBlueprint = null,
        Action<GameObject> configureAfterCopy = null
    )
    {
        if (profile == null || nodes == null || nodes.Count < 2)
        {
            Debug.LogError("[RopeFactory] Invalid arguments");
            return null;
        }

        GameObject go = new GameObject($"Rope_Runtime_{(profile != null ? profile.name : "Unknown")}");
        // Optionally set parent here if needed by caller

        // Add core rope component
        var rope = go.AddComponent<VerletRope7>();

        // Copy module components from blueprint before initialization
        if (logicBlueprint != null)
            CopyLogicModules(logicBlueprint, go);

        // Allow caller to wire adapters or references BEFORE InitializeRuntime is called
        configureAfterCopy?.Invoke(go);

        // Initialize runtime data (this will call InitializeModules inside MarkReady)
        rope.InitializeRuntime(profile, nodes, start, end);

        return rope;
    }

    /// <summary>
    /// Copy module components from src (blueprint) to dst (runtime GameObject).
    /// Skips VerletRope7 type and editor-only types.
    /// Attempts to call CopyConfigFrom on new component if available; otherwise does shallow field copy.
    /// </summary>
    private static void CopyLogicModules(GameObject src, GameObject dst)
    {
        if (src == null || dst == null) return;

        var modules = src.GetComponents<MonoBehaviour>();
        foreach (var module in modules)
        {
            if (module == null) continue;

            var type = module.GetType();

            // Skip core types
            if (typeof(VerletRope7).IsAssignableFrom(type)) continue;

            // Defensive: skip editor-only components
            if (type.FullName != null && (type.FullName.StartsWith("UnityEditor") || type.FullName.Contains("Editor")))
                continue;

            // Add component of same type to destination
            Component newComp = null;
            try
            {
                newComp = dst.AddComponent(type);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RopeFactory] Failed AddComponent for {type.Name}: {ex.Message}");
                continue;
            }

            // Try calling CopyConfigFrom(src) if exists
            try
            {
                var copyMethod = type.GetMethod("CopyConfigFrom", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (copyMethod != null)
                {
                    // invoke with the original module as argument
                    copyMethod.Invoke(newComp, new object[] { module });
                    continue;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RopeFactory] CopyConfigFrom failed for {type.Name}: {ex.Message}");
                // fallthrough to shallow copy
            }

            // Fallback: shallow copy simple serializable fields
            try
            {
                ShallowCopySerializableFields(module, newComp);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RopeFactory] Shallow copy failed for {type.Name}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Shallow-copy simple serializable fields from src component to dst component.
    /// Accepts Component types (not restricted to MonoBehaviour) to match AddComponent return type.
    /// This intentionally avoids copying UnityEngine.Object references (scene objects, prefabs) to prevent
    /// dangling references to blueprint scene.
    /// </summary>
    private static void ShallowCopySerializableFields(Component src, Component dst)
    {
        if (src == null || dst == null) return;

        var type = src.GetType();
        // Use BindingFlags to get public and serialized private fields
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var f in fields)
        {
            // Skip fields not serialized by Unity (or marked [NonSerialized])
            if (f.IsNotSerialized) continue;

            var ft = f.FieldType;

            // Only copy safe simple/value types and common structs used in Unity
            bool safeType =
                ft.IsPrimitive ||
                ft.IsEnum ||
                ft == typeof(string) ||
                ft == typeof(Vector2) ||
                ft == typeof(Vector3) ||
                ft == typeof(Vector4) ||
                ft == typeof(Color) ||
                ft == typeof(Quaternion) ||
                ft == typeof(Rect) ||
                ft == typeof(Bounds) ||
                ft.IsValueType;

            if (!safeType) continue; // skip UnityEngine.Object references and complex types

            try
            {
                var val = f.GetValue(src);
                f.SetValue(dst, val);
            }
            catch
            {
                // ignore individual field copy failures
            }
        }
    }
}
