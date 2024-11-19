using System.Linq;
using UnityEngine;

namespace _3D {

    public class FluidCollider : MonoBehaviour {

        [Header("Collider Settings")]
        [SerializeField, Range(3, 30)] int numSides = 4;
        [SerializeField, Range(0f, 360)] float initialAngle = 0f;
        public BounceDirection bounceDirection = BounceDirection.OUTSIDE;

        [Header("Display Settings")]
        [SerializeField] bool drawCollider = true;
        public bool drawColliderAABB = true;

        [Header("Computed Collider")]
        [SerializeField] Mesh mesh = null;
        [SerializeField] AABB minAABB;

        bool needsUpdate = true;


        public (Vector2[] vertices, Vector2[] normals, AABB aabb) GetData() {
            UpdateSettings();
            Vector2[] vertices = mesh.vertices.Select(v => (Vector2)transform.localToWorldMatrix.MultiplyPoint(v)).ToArray();
            Vector2[] normals = new Vector2[vertices.Length];

            for (int i = 0; i < normals.Length; i++) {
                Vector2 edge = vertices[(i + 1) % normals.Length] - vertices[i];
                normals[i] = new Vector2(edge.y, - edge.x).normalized;
            }

            return (vertices, normals, minAABB);
        }

        private void OnValidate() {
            needsUpdate = true;
        }

        private void UpdateSettings() {
            if (!needsUpdate) { return; }

            mesh = ConvexMeshGenerator.GenerateMesh(numSides, initialAngle * Mathf.Deg2Rad);
            minAABB = ConvexMeshGenerator.GetMinimumAABB(mesh, transform.localToWorldMatrix);
            needsUpdate = false;
        }

        private void OnDrawGizmos() {
            UpdateSettings();
            if (drawCollider) { DrawCollider(); }
            if (drawColliderAABB) { DrawColliderAABB(); }
        }

        private void DrawCollider() {
            var matrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.gray;


            if (bounceDirection == BounceDirection.OUTSIDE) {
                Gizmos.DrawMesh(mesh);
            }
            else {
                Gizmos.DrawLineStrip(mesh.vertices, true);
            }

            Gizmos.matrix = matrix;
        }

        private void DrawColliderAABB() {
            Vector2 size = minAABB.max - minAABB.min;
            Vector2 centre = minAABB.min + size * 0.5f;
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(centre, size);
        }
    }

}