// Assets/Scripts/AnimationSystem/Runtime/IAnimationClipLoader.cs
using System.Threading.Tasks;
using UnityEngine;

public interface IAnimationClipLoader
{
    Task<AnimationClip> Load(AnimationClipRef clipRef);
    void Release(AnimationClipRef clipRef);
}