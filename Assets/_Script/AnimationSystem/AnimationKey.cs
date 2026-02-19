// Assets/Scripts/AnimationSystem/Runtime/AnimationKey.cs
using System;

[Serializable]
public struct AnimationKey : IEquatable<AnimationKey>
{
    public string characterId;
    public string intent;
    public string variant;

    public AnimationKey(string characterId, string intent, string variant = "")
    {
        this.characterId = (characterId ?? "").Trim();
        this.intent = (intent ?? "").Trim();
        this.variant = (variant ?? "").Trim();
    }

    public bool IsValid => !string.IsNullOrWhiteSpace(characterId) && !string.IsNullOrWhiteSpace(intent);

    public string ToId()
    {
        if (string.IsNullOrEmpty(variant)) return $"{characterId}/{intent}";
        return $"{characterId}/{intent}/{variant}";
    }

    public override string ToString() => ToId();

    public bool Equals(AnimationKey other)
    {
        return string.Equals(characterId, other.characterId, StringComparison.Ordinal)
               && string.Equals(intent, other.intent, StringComparison.Ordinal)
               && string.Equals(variant, other.variant, StringComparison.Ordinal);
    }

    public override bool Equals(object obj) => obj is AnimationKey other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + (characterId?.GetHashCode() ?? 0);
            hash = hash * 31 + (intent?.GetHashCode() ?? 0);
            hash = hash * 31 + (variant?.GetHashCode() ?? 0);
            return hash;
        }
    }
}