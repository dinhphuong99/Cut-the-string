// SimpleRopeMesh.cs
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SimpleRopeMeshVerletRope : MonoBehaviour
{
    [SerializeField] private VerletRope7 rope; // tham chiếu vào rope solver

    public float ropeHalfWidth = 0.05f;

    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;
    private Vector2[] uvs;

    private void Awake()
    {
        mesh = new Mesh();
        mesh.name = "RopeMesh";
        GetComponent<MeshFilter>().sharedMesh = mesh;
        mesh.MarkDynamic();
    }

    private void LateUpdate()
    {
        var nodes = rope?.nodes; // property bên rope
        if (nodes == null || nodes.Count < 2) return;
        BuildMesh(nodes);
    }

    void BuildMesh(List<RopeNode4> nodes)
    {
        int segCount = nodes.Count - 1;
        int vertCount = segCount * 2;

        if (vertices == null || vertices.Length != vertCount)
        {
            vertices = new Vector3[vertCount];
            uvs = new Vector2[vertCount];
            triangles = new int[segCount * 6];
        }

        int v = 0, t = 0;
        float uvY = 0f;

        for (int i = 0; i < segCount; i++)
        {
            Vector2 a2 = nodes[i].position;
            Vector2 b2 = nodes[i + 1].position;

            Vector3 a = new Vector3(a2.x, a2.y, 0f);
            Vector3 b = new Vector3(b2.x, b2.y, 0f);

            Vector3 dir = (b - a).normalized;
            Vector3 perp = new Vector3(-dir.y, dir.x, 0f);

            vertices[v + 0] = a + perp * ropeHalfWidth;
            vertices[v + 1] = a - perp * ropeHalfWidth;

            uvs[v + 0] = new Vector2(0, uvY);
            uvs[v + 1] = new Vector2(1, uvY);

            if (i < segCount - 1)
            {
                triangles[t++] = v + 0;
                triangles[t++] = v + 2;
                triangles[t++] = v + 1;

                triangles[t++] = v + 1;
                triangles[t++] = v + 2;
                triangles[t++] = v + 3;
            }

            uvY += Vector3.Distance(a, b) * 2f;
            v += 2;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}
