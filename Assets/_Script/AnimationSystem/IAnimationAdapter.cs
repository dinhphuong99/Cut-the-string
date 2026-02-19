// Assets/Scripts/AnimationSystem/Runtime/IAnimationAdapter.cs
using UnityEngine;

public interface IAnimationAdapter
{
    void PlayClip(AnimationClip clip, float playbackSpeed, float crossfadeSeconds, bool upperBody = false);
    void HardStop();
    void UpdatePlaybackSpeed(float playbackSpeed);
}