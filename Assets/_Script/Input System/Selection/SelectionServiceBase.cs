using System.Collections.Generic;
using UnityEngine;

namespace Game.Selection
{
    public abstract class SelectionServiceBase : MonoBehaviour
    {
        [Header("Core")]
        public Camera selectionCamera;
        public SelectionScorer scorer;
        public bool blockWhenPointerOverUI = true;

        protected readonly List<ISelectable> candidates = new();
        protected ISelectable current;
        public ISelectable Current => current;

        protected virtual void Awake()
        {
            if (selectionCamera == null) selectionCamera = Camera.main;
            if (scorer == null) Debug.LogWarning($"{name}: Scorer not assigned.");
        }

        public void RequestSelection(Vector2 screenPosition)
        {
            if (blockWhenPointerOverUI && UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                ClearSelection();
                return;
            }

            if (selectionCamera == null || scorer == null) return;
            var ctx = new SelectionContext(selectionCamera, screenPosition);
            candidates.Clear();
            QueryCandidates(ctx);

            ISelectable best = null;
            float bestScore = float.NegativeInfinity;
            foreach (var c in candidates)
            {
                float s = scorer.Score(ctx, c);
                if (s > bestScore)
                {
                    bestScore = s;
                    best = c;
                }
            }

            if (best != current)
            {
                current?.OnDeselected();
                current = best;
                current?.OnSelected();
            }
        }

        public void ClearSelection()
        {
            current?.OnDeselected();
            current = null;
        }

        protected abstract void QueryCandidates(in SelectionContext context);
    }
}
