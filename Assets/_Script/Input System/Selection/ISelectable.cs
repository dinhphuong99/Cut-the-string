using UnityEngine;

namespace Game.Selection
{
    public interface ISelectable
    {
        Transform Transform { get; }
        int Priority { get; }
        bool IsSelectable { get; }
        void OnSelected();
        void OnDeselected();
    }
}
