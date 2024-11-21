using UnityEngine;
using System.Linq;

public class Collision3D : MonoBehaviour
{
    [Header("Collider info")]
    [SerializeField] Vector3[] vertices;
    [SerializeField] Vector3[] normals;
    [SerializeField] Vector3Int[] triangles;

    [SerializeField] const int numVertices = 8;
    [SerializeField] const int numFaces = 12;

    [Header("Draw Settings")]
    [SerializeField] bool drawNormals = true;
    
    [Header("References")]
    [SerializeField] Particle3D particle;
    [SerializeField] ComputeShader computeShader;

    // Compute buffers
    ComputeBuffer verticesBuffer;
    ComputeBuffer normalsBuffer;
    ComputeBuffer trianglesBuffer;
    ComputeBuffer collisionResultsBuffer;


    private void InitializeBuffers() {
        verticesBuffer = new ComputeBuffer(numVertices, sizeof(float) * 3);
        normalsBuffer = new ComputeBuffer(numFaces, sizeof(float) * 3);
        trianglesBuffer = new ComputeBuffer(numFaces, sizeof(int) * 3);
        collisionResultsBuffer = new ComputeBuffer(1, sizeof(int));

        GraphicsHelper.SetBufferKernels(computeShader, "_Vertices", verticesBuffer, 0);
        GraphicsHelper.SetBufferKernels(computeShader, "_Normals", normalsBuffer, 0);
        GraphicsHelper.SetBufferKernels(computeShader, "_Triangles", trianglesBuffer, 0);
        GraphicsHelper.SetBufferKernels(computeShader, "_CollisionResults", collisionResultsBuffer, 0);

        verticesBuffer.SetData(vertices);
        normalsBuffer.SetData(normals);
        trianglesBuffer.SetData(triangles);

        computeShader.SetVector("_position", particle.transform.position);
        computeShader.SetFloat("_radius", particle.radius);

    }

    private void DestroyBuffers() {
        verticesBuffer?.Release();
        normalsBuffer?.Release();
        trianglesBuffer?.Release();
        collisionResultsBuffer?.Release();
    }

    private bool CheckCollision() {
        int numGroups = GraphicsHelper.ComputeThreadGroups1D(1);
        computeShader.Dispatch(0, numGroups, 1, 1);
        int[] results = new int[1];
        collisionResultsBuffer.GetData(results);
        return results[0] == 1;
    }

    private void OnDrawGizmos() {
        ResetInfo();
        InitializeBuffers();

        bool collided = CheckCollision();

        for (int i = 0; i < numFaces; i++) {
            int[] vertexIdx = new int[] { triangles[i].x, triangles[i].y, triangles[i].z };
            Vector3[] triangleVertices = vertexIdx.Select(idx => vertices[idx]).ToArray();
            Vector3 triangleCentre = (triangleVertices[0] + triangleVertices[1] + triangleVertices[2]) / 3;
            Gizmos.color = collided ? Color.magenta : Color.gray;
            Gizmos.DrawLineStrip(triangleVertices, true);

            if (drawNormals) {
                Vector3 normal = normals[i];
                Gizmos.color = new Vector4(normal.x, normal.y, normal.z, 1.0f);
                Gizmos.DrawLine(triangleCentre, triangleCentre + normal / 2);
            }
        }

        DestroyBuffers();
    }

    void CalculateNormals() {
        for (int i = 0; i < numFaces; i++) {
            int[] vertexIdx = new int[] { triangles[i].x, triangles[i].y, triangles[i].z };
            Vector3[] tVertices = vertexIdx.Select(idx => vertices[idx]).ToArray();
            normals[i] = Vector3.Cross(tVertices[1] - tVertices[0], tVertices[2] - tVertices[0]).normalized;
        }
    }

    void ResetInfo() {
        vertices = new Vector3[numVertices] {
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f,  0.5f, -0.5f),
            new Vector3(-0.5f,  0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f,  0.5f),
            new Vector3( 0.5f, -0.5f,  0.5f),
            new Vector3( 0.5f,  0.5f,  0.5f),
            new Vector3(-0.5f,  0.5f,  0.5f)
        };
        transform.TransformPoints(vertices);
        
        CalculateNormals();
        triangles = new Vector3Int[numFaces] {
            new Vector3Int(0, 2, 1),
            new Vector3Int(0, 3, 2),
            new Vector3Int(0, 4, 3),
            new Vector3Int(4, 7, 3),
            new Vector3Int(4, 6, 7),
            new Vector3Int(4, 5, 6),
            new Vector3Int(5, 2, 6),
            new Vector3Int(5, 1, 2),
            new Vector3Int(5, 4, 1),
            new Vector3Int(1, 4, 0),
            new Vector3Int(2, 3, 7),
            new Vector3Int(2, 7, 6)
        };
    }
}
