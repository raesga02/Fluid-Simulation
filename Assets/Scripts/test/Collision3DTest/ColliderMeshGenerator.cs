using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

[StructLayout(LayoutKind.Sequential)]
public struct AABB {
    public Vector3 min;
    public Vector3 max;
}

[System.Serializable]
public static class ColliderMeshGenerator {

    public static Mesh GenerateMesh() {
        Vector3[] vertices = GetVertices();
        int[] triangles = GetTriangles();
        Vector3[] normals = Enumerable.Repeat(new Vector3(0.0f, 0.0f, 1.0f), vertices.Length).ToArray();

        return new Mesh { 
            name = "Box",
            vertices = vertices,
            triangles = triangles,
            normals = normals
        };
    }

    public static Vector3[] GetFaceNormals(Mesh mesh) {
        Vector3[] normals = new Vector3[mesh.triangles.Length / 3];

        for (int i = 0, j = 0; i < normals.Length; i++, j+=3) {
            Vector3[] triangleVertices = new int[] { mesh.triangles[j], mesh.triangles[j + 1], mesh.triangles[j + 2] }.Select(idx => mesh.vertices[idx]).ToArray();
            normals[i] = Vector3.Cross(triangleVertices[1] - triangleVertices[0], triangleVertices[2] - triangleVertices[0]).normalized;
        }

        return normals;
    }

    private static Vector3[] GetVertices() {
        Vector3[] vertices = new Vector3[] {
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f,  0.5f, -0.5f),
            new Vector3(-0.5f,  0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f,  0.5f),
            new Vector3( 0.5f, -0.5f,  0.5f),
            new Vector3( 0.5f,  0.5f,  0.5f),
            new Vector3(-0.5f,  0.5f,  0.5f)
        };
        
        return vertices;
    }

    private static int[] GetTriangles() {
        int[] triangles = new int[] {
            0, 2, 1,
            0, 3, 2,
            0, 4, 3,
            4, 7, 3,
            4, 6, 7,
            4, 5, 6,
            5, 2, 6,
            5, 1, 2,
            5, 4, 1,
            1, 4, 0,
            2, 3, 7,
            2, 7, 6
        };

        return triangles;
    }

    public static AABB GetMinimumAABB(Mesh mesh, Matrix4x4 localToWorld) {
        Vector3[] vertices = mesh.vertices.Select(v => localToWorld.MultiplyPoint(v)).ToArray();
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        foreach (Vector3 v in vertices) {
            min = new Vector3(Mathf.Min(min.x, v.x), Mathf.Min(min.y, v.y), Mathf.Min(min.z, v.z));
            max = new Vector3(Mathf.Max(max.x, v.x), Mathf.Max(max.y, v.y), Mathf.Max(max.z, v.z));
        }

        return new AABB() { min = min, max = max };
    }
}