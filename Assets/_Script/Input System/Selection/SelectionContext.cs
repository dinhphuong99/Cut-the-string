using UnityEngine;

namespace Game.Selection
{
    public readonly struct SelectionContext
    {
        public readonly Camera Camera;
        public readonly Vector2 ScreenPosition;

        public SelectionContext(Camera camera, Vector2 screenPosition)
        {
            Camera = camera;
            ScreenPosition = screenPosition;
        }
    }
}
