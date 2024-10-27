using UnityEngine;

public class TestSAT : MonoBehaviour
{

    public Vector2 size;
    public TestPoint point;

    public Vector2[] vertex;
    public bool overlap;
    public float damping = 1f;


    private void OnDrawGizmos() {
        vertex = new Vector2[4] {
            transform.position + new Vector3(  size.x / 2 + 1,   size.y / 2, 0f),
            transform.position + new Vector3(- size.x / 2 - 1,   size.y / 2 - 1, 0f),
            transform.position + new Vector3(- size.x / 2, - size.y / 2, 0f),
            transform.position + new Vector3(  size.x / 2, - size.y / 2, 0f)
        };

        Vector2[] points = new Vector2[2] {
            point.transform.position - new Vector3(point.radius, point.radius, 0),
            point.transform.position + new Vector3(point.radius, point.radius, 0)
        };

        overlap = Collision.CheckOverlap(points, vertex);

        DrawPolygon(overlap ? Color.red : Color.white);


        // Get position to move to to resolve
        if (overlap) {
            Vector2 moveVector = Collision.MTVBetween(points, vertex);

            // Draw moved particle
            Vector2 movedPoint = point.transform.position + new Vector3(moveVector.x, moveVector.y, 0);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(movedPoint, 1f);

            // Draw move normal
            Gizmos.color = Color.gray;
            Gizmos.DrawLine(Vector2.zero, moveVector.normalized);

            // Draw velocity reflected
            Vector2 reflectedVelocity = point.velocity - (damping + 1) * Vector2.Dot(point.velocity, moveVector.normalized) * moveVector.normalized;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(movedPoint, movedPoint + reflectedVelocity);
        }
    }

    void DrawPolygon(Color color) {
        Gizmos.color = color;
        for (int i = 0; i < vertex.Length; i++) {
            Gizmos.DrawLine(vertex[i], vertex[(i + 1) % vertex.Length]);
        }
    }
}

public static class Collision
{
    private struct ProjectionLine {
        public float start;
        public float end;
    }

    private static Vector2 NormalBetween(Vector2 v1, Vector2 v2) => new Vector2(v2.y - v1.y, - (v2.x - v1.x)).normalized;

    private static ProjectionLine ProjectLine(Vector2[] vertices, Vector2 normal) {
        ProjectionLine projectionLine = new ProjectionLine() { start = float.MaxValue, end = float.MinValue };

        foreach (Vector2 v in vertices) {
            float projectionScale = Vector2.Dot(v, normal);
            projectionLine.start = Mathf.Min(projectionScale, projectionLine.start);
            projectionLine.end = Mathf.Max(projectionScale, projectionLine.end);
        }

        return projectionLine;
    }

    private static ProjectionLine ProjectLine(Vector2 point, Vector2 normal) {
        float projectionScale = Vector2.Dot(point, normal);
        return new ProjectionLine() { start = projectionScale, end = projectionScale };
    }

    public static bool CheckOverlap(Vector2[] point, Vector2[] collider) {
        for (int i = 0; i < collider.Length; i++) {
            Vector2 edgeNormal = NormalBetween(collider[i], collider[(i + 1) % collider.Length]);
            ProjectionLine pointProjection = ProjectLine(point, edgeNormal);
            ProjectionLine colliderProjection = ProjectLine(collider, edgeNormal);

            if (!(pointProjection.start <= colliderProjection.end && pointProjection.end >= colliderProjection.start)) {
                return false;
            }
        }

        return true;
    }

    private static float? CollisionResponseAcrossLine(ProjectionLine pointProjection, ProjectionLine colliderProjection)
    {
        if (pointProjection.start <= colliderProjection.start && pointProjection.end >= colliderProjection.start)
            return colliderProjection.start - colliderProjection.end;
        else if (colliderProjection.start <= pointProjection.start && colliderProjection.end >= pointProjection.start)
            return colliderProjection.end - pointProjection.start;
        return null;
    }

    public static Vector2 MTVBetween(Vector2[] point, Vector2[] collider)
    {
        if (!CheckOverlap(point, collider)) { return Vector2.zero; }

        float minResponseMagnitude = float.MaxValue;
        Vector2 responseNormal = Vector2.zero;

        for (int c = 0; c < collider.Length; c++)
        {
            Vector2 cEdgeNormal = NormalBetween(collider[c], collider[(c + 1) % collider.Length]);
            ProjectionLine pointProjected = ProjectLine(point, cEdgeNormal);
            ProjectionLine colliderProjected = ProjectLine(collider, cEdgeNormal);

            float? responseMagnitude = CollisionResponseAcrossLine(pointProjected, colliderProjected);
            if (responseMagnitude != null && Mathf.Abs((float)responseMagnitude) < Mathf.Abs((float)minResponseMagnitude))
            {
                minResponseMagnitude = (float)responseMagnitude;
                responseNormal = cEdgeNormal;
            }
        }

        return responseNormal * Mathf.Abs(minResponseMagnitude);
    }
}
