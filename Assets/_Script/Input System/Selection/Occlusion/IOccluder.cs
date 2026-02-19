using UnityEngine;

namespace Game.Selection
{
    /// <summary>
    /// Marker interface for objects that can block selection ray
    /// but are NOT selectable themselves.
    /// </summary>
    public interface IOccluder
    {
        /// <summary>
        /// Whether this object should block selection ray.
        /// Useful for dynamic occlusion (doors, glass, etc).
        /// </summary>
        bool BlocksSelection { get; }
    }
}
