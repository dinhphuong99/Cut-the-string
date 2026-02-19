// Assets/Scripts/AnimationSystem/Runtime/AnimationDatabase.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AnimationDatabase", menuName = "AnimationSystem/AnimationDatabase")]
public class AnimationDatabase : ScriptableObject
{
    [Tooltip("Populate via CSV importer or editor tool. Do not edit at runtime.")]
    public List<AnimationEntry> entries = new List<AnimationEntry>();

    private Dictionary<string, AnimationClipRef> _cache;

    public void RebuildCache()
    {
        _cache = new Dictionary<string, AnimationClipRef>(entries.Count);
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e == null) continue;
            if (!e.key.IsValid) continue;
            _cache[e.key.ToId()] = e.clipRef;
        }
    }

    public bool TryGetClipRef(AnimationKey key, out AnimationClipRef clipRef)
    {
        if (_cache == null) RebuildCache();
        clipRef = default;
        if (!key.IsValid) return false;
        return _cache.TryGetValue(key.ToId(), out clipRef);
    }

#if UNITY_EDITOR
    // Editor helper to set entries
    public void SetEntries(List<AnimationEntry> newEntries)
    {
        entries = newEntries ?? new List<AnimationEntry>();
        RebuildCache();
    }
#endif
}