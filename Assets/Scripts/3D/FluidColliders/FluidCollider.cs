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

        private LineRenderer lineRenderer = null;


        // Private fields
        public bool needsUpdate = true;

        private void Start() {
            Debug.Log("AAA");
            aabb = ColliderMeshGenerator.GetMinimumAABB(mesh, transform.localToWorldMatrix);
            Debug.Log("BBBB");

            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null) {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = lineRenderer.endColor = Color.black;
            lineRenderer.loop = false;
            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = 0.2f;
            lineRenderer.endWidth = 0.2f;

            Vector3[] points = GetWireCubePoints();
            lineRenderer.positionCount = points.Length;
            lineRenderer.SetPositions(points);
        }

        private void Update() {
            UpdateSettings();

            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            Material material = meshRenderer.sharedMaterial;

            meshRenderer.enabled = bounceDirection == BounceDirection.OUTSIDE;

            if (lineRenderer != null) {
                lineRenderer.enabled = bounceDirection == BounceDirection.INSIDE;
            }

            material.SetColor("_MainColor", solidColliderColor);
            material.SetColor("_LightColor", sceneLight.color * sceneLight.intensity);
            material.SetVector("_LightDirection", -sceneLight.transform.forward);
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

            Vector3[] points = GetWireCubePoints();
            if (lineRenderer != null) {
                lineRenderer.SetPositions(points);
            }

            mesh = ColliderMeshGenerator.GenerateMesh();
            aabb = ColliderMeshGenerator.GetMinimumAABB(mesh, transform.localToWorldMatrix);
            needsUpdate = false;
        }

        Vector3[] GetWireCubePoints()
        {
            Vector3 size = aabb.max - aabb.min;
            Vector3 centre = aabb.min + size * 0.5f;
            Vector3 half = size * 0.5f;

            // 8 corners of the cube
            Vector3 p0 = centre + new Vector3(-half.x, -half.y, -half.z);
            Vector3 p1 = centre + new Vector3(half.x, -half.y, -half.z);
            Vector3 p2 = centre + new Vector3(half.x, -half.y, half.z);
            Vector3 p3 = centre + new Vector3(-half.x, -half.y, half.z);
            Vector3 p4 = centre + new Vector3(-half.x, half.y, -half.z);
            Vector3 p5 = centre + new Vector3(half.x, half.y, -half.z);
            Vector3 p6 = centre + new Vector3(half.x, half.y, half.z);
            Vector3 p7 = centre + new Vector3(-half.x, half.y, half.z);

            // 12 edges (24 points)
            return new Vector3[]
            {
                p0, p1, p1, p2, p2, p3, p3, p0, // bottom square
                p0, p4, p4, p5, p5, p6, p6, p7, p7, p4, // top square
                p4, p5, p5, p1, p1, p2, p2, p6, p6, p7, p7, p3 // Vertical lines
            };
        }

        private void OnDrawGizmos() {
            UpdateSettings();

            if (drawColliderAABB) { DrawColliderAABB(); }
        }

        private void DrawColliderAABB() {
            Vector3 size = aabb.max - aabb.min;
            Vector3 centre = aabb.min + size * 0.5f;
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(centre, size);
        }
    }

}