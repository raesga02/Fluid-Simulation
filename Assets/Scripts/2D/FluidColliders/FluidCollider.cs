using System.Linq;
using UnityEngine;

namespace _2D {

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

        public bool needsUpdate = true;
        public MeshFilter meshFilter = null;
        public MeshRenderer meshRenderer = null;
        
        private LineRenderer lineRenderer = null;

        private void Awake() {
            // Inicializa el LineRenderer si no existe
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null) {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                lineRenderer.widthMultiplier = 0.1f;
                lineRenderer.loop = true;
                lineRenderer.useWorldSpace = false;
                lineRenderer.positionCount = 0;
                lineRenderer.startColor = lineRenderer.endColor = new Color(0.15f, 0.15f, 0.15f);
            }
        }


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
            if (meshFilter != null) {
                meshFilter.mesh = mesh;
            }
            needsUpdate = false;
        }

        private void Update() {
            UpdateSettings();
            
            if (bounceDirection == BounceDirection.OUTSIDE) {
                meshRenderer.enabled = true;
                lineRenderer.enabled = false;
            }
            else {
                meshRenderer.enabled = false;
                lineRenderer.enabled = true;
            }

            if (bounceDirection == BounceDirection.INSIDE) {
                var verts = mesh.vertices;
                lineRenderer.positionCount = verts.Length;
                lineRenderer.enabled = true;

                for (int i = 0; i < verts.Length; i++) {
                    lineRenderer.SetPosition(i, verts[i]);
                }
            }
        }

        private void OnDrawGizmos() {
            if (Application.isPlaying) { return; }
            UpdateSettings();
            if (drawCollider) { DrawCollider(); }
            if (drawColliderAABB) { DrawColliderAABB(); }
        }

        private void DrawCollider() {
            var matrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.15f, 0.15f, 0.15f);

            if (bounceDirection == BounceDirection.OUTSIDE) {
                // Gizmos.DrawMesh(mesh);
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