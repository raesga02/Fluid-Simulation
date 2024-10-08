using UnityEngine;

[System.Serializable]
public static class FluidMeshGenerator2D {

    public static Mesh GenerateMesh(int numSides) {
        return new Mesh { 
            name = "Circle n = " + numSides,
            vertices = GetVertices(numSides),
            triangles = GetTriangles(numSides)
        };
    }

    private static Vector3[] GetVertices(int numSides) {
        Vector3[] vertices = new Vector3[numSides];
        float progressPerStep = 1.0f / numSides;
        float radianPerStep = progressPerStep * Mathf.PI * 2;

        for (int i = 0; i < numSides; i++) {
            float progress = radianPerStep * i;
            vertices[i] = new Vector3(Mathf.Cos(progress), Mathf.Sin(progress), 0f);
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
}
