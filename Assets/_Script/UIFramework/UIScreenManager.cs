using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UIFramework
{
    [DefaultExecutionOrder(100)]
    public class UIScreenManager : MonoBehaviour
    {
        public static UIScreenManager Instance { get; private set; }

        [Tooltip("Prefabs for screens. Ensure screenId is set on each UIScreen prefab.")]
        public List<UIScreen> screenPrefabs = new List<UIScreen>();

        private Dictionary<string, UIScreen> instantiated = new Dictionary<string, UIScreen>();
        private Stack<UIScreen> stack = new Stack<UIScreen>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            // Validate prefabs with validator
            foreach (var p in screenPrefabs)
            {
                if (p == null) continue;
                if (string.IsNullOrEmpty(p.screenId))
                {
                    Debug.LogWarning($"UIScreen prefab {p.name} has empty screenId.");
                }
            }
        }

        public bool HasScreen(string id) => instantiated.ContainsKey(id) || screenPrefabs.Exists(p => p != null && p.screenId == id);

        public UIScreen GetScreenInstance(string id)
        {
            if (instantiated.TryGetValue(id, out var s)) return s;
            var prefab = screenPrefabs.Find(p => p != null && p.screenId == id);
            if (prefab == null) return null;
            var go = Instantiate(prefab, transform);
            var screen = go.GetComponent<UIScreen>();
            instantiated[id] = screen;
            return screen;
        }

        public void Push(string id)
        {
            StartCoroutine(PushInternal(id));
        }

        private IEnumerator PushInternal(string id)
        {
            var newScreen = GetScreenInstance(id);
            if (newScreen == null)
            {
                Debug.LogError($"Push failed: screen id '{id}' not found.");
                yield break;
            }

            // Pause previous if needed
            if (stack.Count > 0)
            {
                var cur = stack.Peek();
                if (cur != null)
                {
                    cur.Pause();
                }
            }

            stack.Push(newScreen);

            // handle input blocking: if modal, it will block underlying automatically via CanvasGroup
            yield return StartCoroutine(newScreen.Enter());
        }

        public void Pop()
        {
            StartCoroutine(PopInternal());
        }

        private IEnumerator PopInternal()
        {
            if (stack.Count == 0) yield break;
            var top = stack.Pop();
            if (top == null) yield break;
            if (!top.CanExit())
            {
                // re-push
                stack.Push(top);
                yield break;
            }
            yield return StartCoroutine(top.Exit());

            // resume underlying
            if (stack.Count > 0)
            {
                var resumed = stack.Peek();
                resumed?.Resume();
            }
        }

        public void Replace(string id)
        {
            StartCoroutine(ReplaceInternal(id));
        }

        private IEnumerator ReplaceInternal(string id)
        {
            // pop current
            if (stack.Count > 0)
            {
                var top = stack.Pop();
                if (top != null)
                {
                    if (!top.CanExit())
                    {
                        stack.Push(top);
                        yield break;
                    }
                    yield return StartCoroutine(top.Exit());
                }
            }
            // push new
            yield return StartCoroutine(PushInternal(id));
        }

        // helper: clear all screens
        public void ClearAll()
        {
            while (stack.Count > 0)
            {
                var s = stack.Pop();
                if (s != null) { s.gameObject.SetActive(false); }
            }
        }

        // For UIInputRouter to query top
        public UIScreen TopScreen => stack.Count > 0 ? stack.Peek() : null;
    }
}
