using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SimpleRopeMesh : MonoBehaviour
{
    public List<Vector3> nodes = new List<Vector3>();
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
    }

    private void LateUpdate()
    {
        if (nodes == null || nodes.Count < 2)
            return;

        BuildMesh();
    }

    void BuildMesh()
    {
        int segCount = nodes.Count - 1;
        int vertCount = segCount * 2;

        if (vertices == null || vertices.Length != vertCount)
        {
            vertices = new Vector3[vertCount];
            uvs = new Vector2[vertCount];
            triangles = new int[segCount * 6];
        }

        int v = 0;
        int t = 0;

        float uvY = 0f;

        for (int i = 0; i < segCount; i++)
        {
            Vector3 a = nodes[i];
            Vector3 b = nodes[i + 1];

            Vector3 dir = (b - a).normalized;

            // perpendicular 2D (x,y) — nếu 3D thì phải tính tangent/bitangent nghiêm túc
            Vector3 perp = new Vector3(-dir.y, dir.x, 0f);

            Vector3 vLeft = a + perp * ropeHalfWidth;
            Vector3 vRight = a - perp * ropeHalfWidth;

            vertices[v + 0] = vLeft;
            vertices[v + 1] = vRight;

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