using UnityEngine;

namespace Game.Input
{
    public struct InputActionValue
    {
        public Vector2 Vector;
        public float Float;
        public bool Bool;
        public string Phase;

        public static InputActionValue FromVector(Vector2 v, string phase = "performed") => new InputActionValue { Vector = v, Phase = phase };
        public static InputActionValue FromFloat(float f, string phase = "performed") => new InputActionValue { Float = f, Phase = phase };
        public static InputActionValue FromButton(bool b, string phase = "performed") => new InputActionValue { Bool = b, Phase = phase };
        public static InputActionValue Empty => new InputActionValue { Vector = default, Float = 0f, Bool = false, Phase = null };
    }
}
