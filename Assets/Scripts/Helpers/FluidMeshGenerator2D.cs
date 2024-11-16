using UnityEngine;

[System.Serializable]
public static class FluidMeshGenerator2D {

    public static Mesh GenerateMesh(int numSides, float initialAngle = 0f) {
        return new Mesh { 
            name = "Circle n = " + numSides,
            vertices = GetVertices(numSides, initialAngle),
            triangles = GetTriangles(numSides)
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
}
