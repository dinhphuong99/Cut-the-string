// Assets/Scripts/AnimationSystem/Runtime/DefaultAnimationClipLoader.cs
using System.Threading.Tasks;
using UnityEngine;

public class DefaultAnimationClipLoader : IAnimationClipLoader
{
    public Task<AnimationClip> Load(AnimationClipRef clipRef)
    {
        if (clipRef.source == AnimationClipSource.Asset)
            return Task.FromResult(clipRef.clip);

        return Task.FromResult<AnimationClip>(null); // no addressables support
    }

    public void Release(AnimationClipRef clipRef) { }
}