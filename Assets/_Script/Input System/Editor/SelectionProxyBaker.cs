#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Game.Selection;

public class SelectionProxyBaker : EditorWindow
{
    [MenuItem("Tools/Selection/Bake Proxies From Selected")]
    static void ShowWindow()
    {
        GetWindow<SelectionProxyBaker>("Selection Proxy Baker");
    }

    void OnGUI()
    {
        if (GUILayout.Button("Bake proxies for selected"))
        {
            foreach (var go in Selection.gameObjects)
            {
                BakeFor(go);
            }
        }
    }

    static void BakeFor(GameObject root)
    {
        var proxyRoot = new GameObject("__SelectionProxies__");
        proxyRoot.transform.SetParent(root.transform, false);
        proxyRoot.layer = LayerMask.NameToLayer("SelectionProxy"); // ensure layer exists

        // Simple: create a child box collider sized to renderer bounds
        var rend = root.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            var proxy = new GameObject("Proxy_Box");
            proxy.transform.SetParent(proxyRoot.transform, false);
            proxy.transform.position = rend.bounds.center;
            var box = proxy.AddComponent<BoxCollider>();
            box.size = rend.bounds.size;
            proxy.AddComponent<SelectionProxy3D>().ownerBehaviour = root.GetComponent<MonoBehaviour>(); // best-effort
            proxy.layer = proxyRoot.layer;
        }
        else
        {
            // 2D sprite
            var spr = root.GetComponentInChildren<SpriteRenderer>();
            if (spr != null)
            {
                var proxy = new GameObject("Proxy_Circle2D");
                proxy.transform.SetParent(proxyRoot.transform, false);
                proxy.transform.position = spr.bounds.center;
                var col = proxy.AddComponent<CircleCollider2D>();
                col.radius = Mathf.Max(spr.bounds.size.x, spr.bounds.size.y) * 0.5f;
                var p = proxy.AddComponent<SelectionProxy2D>();
                p.ownerBehaviour = root.GetComponent<MonoBehaviour>();
                proxy.layer = proxyRoot.layer;
            }
        }

        UnityEditor.EditorUtility.SetDirty(root);
    }
}
#endif