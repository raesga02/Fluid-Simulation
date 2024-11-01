using UnityEngine;

public class FluidCollider2D : MonoBehaviour {

    [Header("Collider Settings")]
    [SerializeField] Vector2 colliderSize = new Vector2(1.0f, 1.0f);
    public BounceDirection bounceDirection = BounceDirection.OUTSIDE;

    [Header("Display Settings")]
    [SerializeField, Range(0.5f, 2.0f)] float markerSeparation = 0.1f;
    [SerializeField, Range(0.0f, 0.5f)] float markerLength = 0.1f;
    [SerializeField] bool drawColliderBounds = true;


    public (Vector2[] vertices, Vector2[] normals) GetData() {
        Vector2 colliderHalfSize = colliderSize / 2;
        Vector2[] vertices = new Vector2[] {
            transform.TransformPoint(colliderHalfSize * new Vector2( 1,  1)),
            transform.TransformPoint(colliderHalfSize * new Vector2(-1,  1)),
            transform.TransformPoint(colliderHalfSize * new Vector2(-1, -1)),
            transform.TransformPoint(colliderHalfSize * new Vector2( 1, -1)),
        };
        Vector2[] normals = new Vector2[vertices.Length];

        for (int i = 0; i < normals.Length; i++) {
            Vector2 edge = vertices[(i + 1) % normals.Length] - vertices[i];
            normals[i] = new Vector2(edge.y, - edge.x).normalized;
        }

        return (vertices, normals);
    }

    void OnDrawGizmos() {
        if (drawColliderBounds) {
            DrawBox();
            DrawCorners();
        }
    }

    void DrawBox() {
        var matrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, colliderSize);
        Gizmos.matrix = matrix;
    }

    void DrawCorners() {
        var matrix = Gizmos.matrix;
        Gizmos.color = new Color(0.7f, 0.4f, 0.05f, 1.0f);

        Vector2[] quadrants = new Vector2[] {
            new Vector2( 1,  1),
            new Vector2(-1,  1),
            new Vector2( 1, -1),
            new Vector2(-1, -1)
        };

        foreach (Vector2 quadrant in quadrants) {
            Vector3 corner = quadrant * (colliderSize + Vector2.one * markerSeparation * (bounceDirection == BounceDirection.INSIDE ? 1 : -1)) * 0.5f;
            Vector2 cornerOffsets = - quadrant * markerLength * (colliderSize + Vector2.one * markerSeparation);
            Gizmos.DrawLine(transform.TransformPoint(corner + new Vector3(cornerOffsets.x, 0, 0)), transform.TransformPoint(corner));
            Gizmos.DrawLine(transform.TransformPoint(corner + new Vector3(0, cornerOffsets.y, 0)), transform.TransformPoint(corner));
        }

        Gizmos.matrix = matrix;
    }
}
