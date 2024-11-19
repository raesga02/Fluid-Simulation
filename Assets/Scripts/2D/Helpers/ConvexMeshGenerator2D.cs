using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace _2D {

    [StructLayout(LayoutKind.Sequential)]
    public struct AABB {
        public Vector2 min;
        public Vector2 max;
    }

    [System.Serializable]
    public static class ConvexMeshGenerator2D {

        public static Mesh GenerateMesh(int numSides, float initialAngle = 0f) {
            return new Mesh { 
                name = "Circle n = " + numSides,
                vertices = GetVertices(numSides, initialAngle),
                triangles = GetTriangles(numSides),
                normals = GetNormals(numSides)
            };
        }

        private static Vector3[] GetVertices(int numSides, float initialAngle = 0f) {
            Vector3[] vertices = new Vector3[numSides];
            float radianPerStep = Mathf.PI * 2f / numSides;

            for (int i = 0; i < numSides; i++) {
                float angle = radianPerStep * i + initialAngle;
                vertices[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            }
            
            return vertices;
        }

        private static int[] GetTriangles(int numSides) {
            int numTriangles = numSides - 2;
            int[] triangles = new int[numTriangles * 3];

            for (int i = 0; i < numTriangles; i++) {
                int j = i * 3;
                triangles[j] = 0;
                triangles[j + 1] = i + 2;
                triangles[j + 2] = i + 1;
            }

            return triangles;
        }

        private static Vector3[] GetNormals(int numSides) {
            return Enumerable.Repeat(new Vector3(0f, 0f, 1f), numSides).ToArray();
        }

        public static AABB GetMinimumAABB(Mesh mesh, Matrix4x4 localToWorld) {
            Vector3[] vertices = mesh.vertices.Select(v => localToWorld.MultiplyPoint(v)).ToArray();
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            foreach (Vector2 v in vertices) {
                min = new Vector2(Mathf.Min(min.x, v.x), Mathf.Min(min.y, v.y));
                max = new Vector2(Mathf.Max(max.x, v.x), Mathf.Max(max.y, v.y));
            }

            return new AABB() { min = min, max = max };
        }
    }
    
}