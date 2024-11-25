using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

public enum BounceDirection {
    OUTSIDE = 0,
    INSIDE = 1,
}

public struct ColliderData {
    public Vector3[] vertices;
    public Vector3[] faceNormals;
    public AABB aabb;
}

[StructLayout(LayoutKind.Sequential)]
public struct ColliderLookup {
    public int startIdx;
    public int numVertices;
    public int numFaces;
    public int isBounds;
    public AABB aabb;
};

public class Collider3D : MonoBehaviour
{
    [Header("Collider Settings")]
    public BounceDirection bounceDirection = BounceDirection.OUTSIDE;
    public bool collided = false;
    public bool aabbCollided = false;

    [Header("Display Settings")]
    [SerializeField] bool drawCollider = true;
    [SerializeField] bool drawNormals = true;
    [SerializeField] bool drawColliderAABB = true;

    // Private fields
    Mesh mesh = null;
    AABB aabb;
    bool needsUpdate = true;


    public ColliderData GetData() {
        UpdateSettings();
        Vector3[] vertices = mesh.vertices.Select(v => transform.TransformPoint(v)).ToArray();
        Vector3[] faceNormals = ColliderMeshGenerator.GetFaceNormals(mesh).Select(n => transform.TransformDirection(n)).ToArray();
        AABB aabb = ColliderMeshGenerator.GetMinimumAABB(mesh, transform.localToWorldMatrix);
    
        return new ColliderData {
            vertices = vertices,
            faceNormals = faceNormals,
            aabb = aabb
        };
    }

    private void OnValidate() {
        needsUpdate = true;
    }

    private void UpdateSettings() {
        if (!needsUpdate) { return; }

        mesh = ColliderMeshGenerator.GenerateMesh();
        aabb = ColliderMeshGenerator.GetMinimumAABB(mesh, transform.localToWorldMatrix);
        needsUpdate = false;
    }

    private void OnDrawGizmos() {
        UpdateSettings();
        if (drawCollider) { DrawCollider(); }
        if (drawNormals) { DrawNormals(); }
        if (drawColliderAABB) { DrawColliderAABB(); }
    }

    private void DrawCollider() {
        var matrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        if (bounceDirection == BounceDirection.OUTSIDE) { 
            Gizmos.color = collided ? new Color(0.5f, 0.5f, 0.25f) : new Color(0.2f, 0.2f, 0.1f);
            Gizmos.DrawWireMesh(mesh);
        }
        else { 
            Gizmos.color = collided ? new Color(0.25f, 0.75f, 0.25f) : new Color(0.1f, 0.5f, 0.1f);
            Gizmos.DrawWireMesh(mesh);
        }

        Gizmos.matrix = matrix;
    }

    private void DrawNormals() {
        Vector3[] faceNormals = ColliderMeshGenerator.GetFaceNormals(mesh);
        
        for (int i = 0, j = i; i < faceNormals.Length; i++, j+=3) {
            var matrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            Vector3 normal = faceNormals[i];
            Vector3 wNormal = transform.TransformDirection(normal);
            Vector3[] triangleVertices = new int[] { mesh.triangles[j], mesh.triangles[j + 1], mesh.triangles[j + 2] }.Select(idx => mesh.vertices[idx]).ToArray();
            Vector3 triangleCentre = triangleVertices.Aggregate(Vector3.zero, (currentSum, nextVertex) => currentSum + nextVertex) / 3;

            Gizmos.color = new Color(wNormal.x, wNormal.y, wNormal.z, 0.75f);
            Gizmos.DrawLine(triangleCentre, triangleCentre + normal / 10);

            Gizmos.matrix = matrix;
        }
    }

    private void DrawColliderAABB() {
        aabb = ColliderMeshGenerator.GetMinimumAABB(mesh, transform.localToWorldMatrix);

        Vector3 size = aabb.max - aabb.min;
        Vector3 centre = aabb.min + size * 0.5f;
        Gizmos.color = aabbCollided ? new Color(0.25f, 0.25f, 0.75f) : new Color(0.1f, 0.1f, 0.5f);
        Gizmos.DrawWireCube(centre, size);
    }

}