using UnityEngine;

namespace Game.Selection
{
    // Optional: implement if you need precise mesh/ray tests
    public interface IPreciseSelectable
    {
        bool PreciseHitTest(Ray worldRay);         // used in 3D refine
        bool PreciseHitTest(Vector2 worldPoint);   // used in 2D refine
    }
}