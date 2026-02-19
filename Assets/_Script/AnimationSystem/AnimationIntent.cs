// Assets/Scripts/AnimationSystem/Runtime/AnimationIntent.cs
using System;

[Serializable]
public struct AnimationIntent
{
    public string intent;          // e.g. "Attack"
    public string variant;         // optional "Light", "Sword"
    public float targetDuration;   // desired gameplay duration in seconds (0 = use clip length)
    public float speedMultiplier;  // extra multiplier

    public AnimationIntent(string intent, string variant = "", float targetDuration = 0f, float speedMultiplier = 1f)
    {
        this.intent = (intent ?? "").Trim();
        this.variant = (variant ?? "").Trim();
        this.targetDuration = targetDuration;
        this.speedMultiplier = speedMultiplier;
    }

    public bool IsValid => !string.IsNullOrWhiteSpace(intent);
}