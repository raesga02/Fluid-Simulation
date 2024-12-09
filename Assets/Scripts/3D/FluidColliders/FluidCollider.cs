using System.Linq;
using UnityEngine;

namespace _3D {
    [ExecuteInEditMode]
    public class FluidCollider : MonoBehaviour {

        [Header("Collider Settings")]
        public BounceDirection bounceDirection = BounceDirection.OUTSIDE;

        [Header("Collider Display Settings")]
        public Light sceneLight;
        public Color solidColliderColor = new Color(0.25f, 0.5f, 0.5f);
        public Color hollowColliderColor = new Color(0.25f, 0.75f, 0.25f);
        public bool drawColliderAABB = true;
        
        [Header("Computed Collider")]
        [SerializeField] Mesh mesh = null;
        [SerializeField] AABB aabb;

        // Private fields
        bool needsUpdate = true;

        private void Update() {
            UpdateSettings();

            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            Material material = meshRenderer.sharedMaterial;

            meshRenderer.enabled = bounceDirection == BounceDirection.OUTSIDE;

            material.SetColor("_MainColor", solidColliderColor);
            material.SetColor("_LightColor", sceneLight.color * sceneLight.intensity);
            material.SetVector("_LightDirection", - sceneLight.transform.forward);
        }

        public ColliderData GetData() {
            UpdateSettings();
            Vector3[] vertices = mesh.vertices.Select(v => transform.TransformPoint(v)).ToArray();
            Vector3[] collisionNormals = ColliderMeshGenerator.GetCollisionNormals(mesh).Select(n => transform.TransformDirection(n)).ToArray();
            AABB aabb = ColliderMeshGenerator.GetMinimumAABB(mesh, transform.localToWorldMatrix);
        
            return new ColliderData {
                vertices = vertices,
                collisionNormals = collisionNormals,
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

            if (drawColliderAABB) { DrawColliderAABB(); }
        }

        private void DrawColliderAABB() {
            aabb = ColliderMeshGenerator.GetMinimumAABB(mesh, transform.localToWorldMatrix);

            Vector3 size = aabb.max - aabb.min;
            Vector3 centre = aabb.min + size * 0.5f;
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(centre, size);
        }
    }

}