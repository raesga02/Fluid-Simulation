using UnityEngine;
using System.Linq;
using System.Runtime.InteropServices;
using System;
using UnityEditor.MPE;

[Serializable]
public struct CollisionDisplacement {
    public float magnitude;
    public Vector3 direction;
    public Vector3 displacement;
};


public class Collision3D : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] bool checkCollision = true;
    [SerializeField] bool checkAABBCollision = true;

    [Header("Collider info")]
    public ColliderData colliderData;
    public CollisionDisplacement[] results;
    public CollisionDisplacement[] aabbResults;

    [Header("References")]
    [SerializeField] Collider3D collider3d;
    [SerializeField] Particle3D particle;
    [SerializeField] ComputeShader computeShader;

    // Compute buffers
    ComputeBuffer verticesBuffer;
    ComputeBuffer faceNormalsBuffer;
    ComputeBuffer collidersLookupsBuffer;
    ComputeBuffer collisionResultsBuffer;
    ComputeBuffer aabbCollisionResultsBuffer;

    private void InitializeBuffers() {
        // Alloc buffers
        verticesBuffer = new ComputeBuffer(colliderData.vertices.Length, sizeof(float) * 3);
        faceNormalsBuffer = new ComputeBuffer(colliderData.faceNormals.Length, sizeof(float) * 3);
        collidersLookupsBuffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(ColliderLookup)));
        collisionResultsBuffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(CollisionDisplacement)));
        aabbCollisionResultsBuffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(CollisionDisplacement)));

        // Bind compute buffers
        GraphicsHelper.SetBufferKernels(computeShader, "_Vertices", verticesBuffer, 0, 1);
        GraphicsHelper.SetBufferKernels(computeShader, "_Normals", faceNormalsBuffer, 0, 1);
        GraphicsHelper.SetBufferKernels(computeShader, "_CollidersLookup", collidersLookupsBuffer, 0, 1);
        GraphicsHelper.SetBufferKernels(computeShader, "_CollisionResults", collisionResultsBuffer, 0, 1);
        GraphicsHelper.SetBufferKernels(computeShader, "_AABBCollisionResults", aabbCollisionResultsBuffer, 0, 1);

        // Initialize data
        verticesBuffer.SetData(colliderData.vertices);
        faceNormalsBuffer.SetData(colliderData.faceNormals);
        collidersLookupsBuffer.SetData(Enumerable.Repeat(new ColliderLookup { 
            startIdx = 0, 
            numVertices = colliderData.vertices.Length, 
            numFaces = colliderData.faceNormals.Length,
            isBounds = collider3d.bounceDirection == BounceDirection.INSIDE ? 1 : 0,
            aabb = colliderData.aabb
        },1).ToArray());
        collisionResultsBuffer.SetData(Enumerable.Repeat(new CollisionDisplacement { magnitude = 0.0f, direction = Vector3.zero, displacement = Vector3.zero}, 1).ToArray());
        aabbCollisionResultsBuffer.SetData(Enumerable.Repeat(new CollisionDisplacement { magnitude = 0.0f, direction = Vector3.zero}, 1).ToArray());

        // Initialize compute shader variables
        computeShader.SetVector("_position", particle.transform.position);
        computeShader.SetFloat("_radius", particle.radius);
        computeShader.SetInt("_numVertices", colliderData.vertices.Length);
        computeShader.SetInt("_numFaces", colliderData.faceNormals.Length);
    }

    private void DestroyBuffers() {
        verticesBuffer?.Release();
        faceNormalsBuffer?.Release();
        collidersLookupsBuffer?.Release();
        collisionResultsBuffer?.Release();
        aabbCollisionResultsBuffer?.Release();
    }

    private CollisionDisplacement CheckCollision() {
        int numGroups = GraphicsHelper.ComputeThreadGroups1D(1);
        computeShader.Dispatch(0, numGroups, 1, 1);
        results = new CollisionDisplacement[1];
        collisionResultsBuffer.GetData(results);
        return results[0];
    }

    private CollisionDisplacement CheckAABBCollision() {
        int numGroups = GraphicsHelper.ComputeThreadGroups1D(1);
        computeShader.Dispatch(1, numGroups, 1, 1);
        aabbResults = new CollisionDisplacement[1];
        aabbCollisionResultsBuffer.GetData(aabbResults);
        return aabbResults[0];
    }

    private void OnDrawGizmos() {
        ResetInfo();
        InitializeBuffers();

        if (checkCollision) {
            CollisionDisplacement response = CheckCollision();
            bool collided = response.magnitude != 0.0f;
            collider3d.collided = collided;

            if (collided) {
                Vector3 newCentre = particle.transform.position + response.displacement;
                Gizmos.color = collider3d.bounceDirection == BounceDirection.OUTSIDE ? Color.yellow : Color.green;
                Gizmos.DrawWireSphere(newCentre, particle.radius);
            }
        }
        else {
            collider3d.collided = false;
        }

        if (checkAABBCollision) {
            CollisionDisplacement aabbResponse = CheckAABBCollision();
            bool aabbCollided = aabbResponse.magnitude != 0.0f;
            collider3d.aabbCollided = aabbCollided;

            if (aabbCollided) {
                Vector3 newCentre = particle.transform.position + aabbResponse.displacement;
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(newCentre, particle.radius);
            }
        }
        else {
            collider3d.aabbCollided = false;
        }

        DestroyBuffers();
    }

    void ResetInfo() {
        colliderData = collider3d.GetData();
    }
}
