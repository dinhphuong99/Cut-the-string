// Assets/Scripts/AnimationSystem/Runtime/AddressablesAnimationClipLoader.cs
#if ENABLE_ADDRESSABLES
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressablesAnimationClipLoader : IAnimationClipLoader
{
    private readonly Dictionary<string, AsyncOperationHandle<AnimationClip>> _handles = new();

    public async Task<AnimationClip> Load(AnimationClipRef clipRef)
    {
        if (clipRef.source == AnimationClipSource.Asset)
            return clipRef.clip;

        if (clipRef.source != AnimationClipSource.Addressable || string.IsNullOrWhiteSpace(clipRef.addressKey))
            return null;

        if (_handles.TryGetValue(clipRef.addressKey, out var existing))
        {
            if (existing.IsValid() && existing.Status == AsyncOperationStatus.Succeeded)
                return existing.Result;

            await existing.Task;
            return existing.Result;
        }

        var handle = Addressables.LoadAssetAsync<AnimationClip>(clipRef.addressKey);
        _handles[clipRef.addressKey] = handle;
        await handle.Task;
        if (handle.Status != AsyncOperationStatus.Succeeded) return null;
        return handle.Result;
    }

    public void Release(AnimationClipRef clipRef)
    {
        if (clipRef.source != AnimationClipSource.Addressable) return;
        if (string.IsNullOrWhiteSpace(clipRef.addressKey)) return;

        if (_handles.TryGetValue(clipRef.addressKey, out var handle))
        {
            if (handle.IsValid())
                Addressables.Release(handle);
            _handles.Remove(clipRef.addressKey);
        }
    }
}
#endif