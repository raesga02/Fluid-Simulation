using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace _3D {

    public struct ColliderData {
        public Vector3[] vertices;
        public Vector3[] collisionNormals;
        public AABB aabb;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ColliderLookup {
        public int vertexStartIdx;
        public int numVertices;
        public int normalStartIdx;
        public int numCollisionNormals;
        public int isBounds;
        public AABB aabb;
    };

    public enum BounceDirection {
        OUTSIDE = 0,
        INSIDE = 1,
    }

    public class FluidColliderManager : MonoBehaviour {
        
        [Header("Fluid Colliders Display Settings")]
        [SerializeField] Color solidColliderColor = new Color(0.5f, 0.5f, 0.25f);
        [SerializeField] Color hollowColliderColor = new Color(0.25f, 0.75f, 0.25f);
        [SerializeField] bool drawCollidersAABB = false;

        [Header("Collider Update Settings")]
        [SerializeField] bool debugUpdateColliders = false;

        [Header("References")]
        public FluidCollider[] colliders;

        public (ColliderLookup[] lookups, Vector3[] vertices, Vector3[] normals) GetColliderData() {
            UpdateColliders();

            ColliderData[] collidersData = colliders.Select(collider => collider.GetData()).ToArray();
            Vector3[] collidersVertices = collidersData.SelectMany(colliderData => colliderData.vertices).ToArray();
            Vector3[] collidersNormals = collidersData.SelectMany(colliderData => colliderData.collisionNormals).ToArray();
            ColliderLookup[] collidersLookups = new ColliderLookup[colliders.Length];
            
            int vertexCurrentIdx = 0;
            int normalCurrentIdx = 0;
            for (int i = 0; i < colliders.Length; i++) {
                ColliderLookup lookup = new ColliderLookup { 
                    vertexStartIdx = vertexCurrentIdx,
                    numVertices = collidersData[i].vertices.Length,
                    normalStartIdx = normalCurrentIdx,
                    numCollisionNormals = collidersData[i].collisionNormals.Length,
                    isBounds = colliders[i].bounceDirection == BounceDirection.INSIDE ? 1 : 0,
                    aabb = collidersData[i].aabb
                };

                collidersLookups[i] = lookup;
                vertexCurrentIdx += lookup.numVertices;
                normalCurrentIdx += lookup.numCollisionNormals;
            }

            return (collidersLookups, collidersVertices, collidersNormals);
        }

        public void UpdateColliders() {
            colliders = GetComponentsInChildren<FluidCollider>();
            foreach (FluidCollider collider in colliders) {
                collider.solidColliderColor = solidColliderColor;
                collider.hollowColliderColor = hollowColliderColor;
                collider.drawColliderAABB = drawCollidersAABB;
            }
        }

        private void OnValidate() {
            if (!debugUpdateColliders) { return; }

            UpdateColliders();
            debugUpdateColliders = false;
        }
    }

}