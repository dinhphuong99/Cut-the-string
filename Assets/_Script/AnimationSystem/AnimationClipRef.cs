// Assets/Scripts/AnimationSystem/Runtime/AnimationClipRef.cs
using System;
using UnityEngine;

public enum AnimationClipSource
{
    Asset = 0,
    Addressable = 1
}

[Serializable]
public struct AnimationClipRef
{
    public AnimationClipSource source;
    public AnimationClip clip;     // direct asset reference (optional)
    public string addressKey;      // addressable key (optional)

    public bool IsValidAsset => source == AnimationClipSource.Asset && clip != null;
    public bool IsValidAddressable => source == AnimationClipSource.Addressable && !string.IsNullOrWhiteSpace(addressKey);

    public override string ToString()
    {
        return source == AnimationClipSource.Asset
            ? (clip ? clip.name : "(null)")
            : $"Addressable:{addressKey}";
    }
}