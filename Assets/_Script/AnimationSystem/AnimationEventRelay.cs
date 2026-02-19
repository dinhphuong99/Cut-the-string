// Assets/Scripts/AnimationSystem/Runtime/AnimationEventRelay.cs
using System;
using UnityEngine;

public class AnimationEventRelay : MonoBehaviour
{
    public event Action<string> OnEvent;
    public void Relay(string id) => OnEvent?.Invoke(id);
}