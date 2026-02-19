// Assets/Scripts/AnimationSystem/Runtime/AnimationEntry.cs
using System;
using UnityEngine;

[Serializable]
public class AnimationEntry
{
    public AnimationKey key;
    public AnimationClipRef clipRef;

    public AnimationEntry() { }

    public AnimationEntry(AnimationKey key, AnimationClipRef clipRef)
    {
        this.key = key;
        this.clipRef = clipRef;
    }
}