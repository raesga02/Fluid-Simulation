using UnityEngine;

public class TestSAT : MonoBehaviour
{

    public Vector2 size;
    public TestPoint point;

    public Vector2[] vertex;
    public bool overlap;
    public float damping = 1f;
    public BounceDirection bounceDir = BounceDirection.INSIDE;


    private void OnDrawGizmos() {
        vertex = new Vector2[4] {
            transform.position + new Vector3(  size.x / 2 + 5,   size.y / 2, 0f),
            transform.position + new Vector3(- size.x / 2 - 1,   size.y / 2 - 1, 0f),
            transform.position + new Vector3(- size.x / 2, - size.y / 2, 0f),
            transform.position + new Vector3(  size.x / 2, - size.y / 2, 0f)
        };

        // Get position to move to to resolve
        Vector2 moveVector = Collision.SATResponse(point.transform.position, vertex, point.radius);
        overlap = !moveVector.Equals(Vector2.zero);

        DrawPolygon(overlap ? Color.red : Color.white);

        if (overlap) {
            // Draw moved particle
            Vector2 movedPoint = point.transform.position + new Vector3(moveVector.x, moveVector.y, 0);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(movedPoint, point.radius);

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

    private static ProjectionLine ProjectPoint(Vector2 point, float radius, Vector2 normal) {
        float projectedPoint = Vector2.Dot(point, normal);
        return new ProjectionLine() { start = projectedPoint - radius, end = projectedPoint + radius };
    }

    public static bool CheckOverlap(Vector2 point, float radius, Vector2[] collider) {
        for (int i = 0; i < collider.Length; i++) {
            Vector2 edgeNormal = NormalBetween(collider[i], collider[(i + 1) % collider.Length]);
            ProjectionLine pointProjection = ProjectPoint(point, radius, edgeNormal);
            ProjectionLine colliderProjection = ProjectLine(collider, edgeNormal);

            if (!(pointProjection.start <= colliderProjection.end && pointProjection.end >= colliderProjection.start)) {
                return false;
            }
        }

        return true;
    }

    private static float? CollisionResponseAcrossLine(ProjectionLine pointProjection, ProjectionLine colliderProjection)
    {
        // If point completely inside of collider
        if (pointProjection.start <= colliderProjection.start && pointProjection.end >= colliderProjection.start)
            return colliderProjection.start - colliderProjection.end;
        // If point partially inside
        else if (colliderProjection.start <= pointProjection.start && colliderProjection.end >= pointProjection.start)
            return colliderProjection.end - pointProjection.start;
        return null;
    }

    public static Vector2 MTVBetween(Vector2 point, Vector2[] collider, float radius) {
        if (!CheckOverlap(point, radius, collider)) { return Vector2.zero; }

        float minResponseMagnitude = float.MaxValue;
        Vector2 responseNormal = Vector2.zero;

        for (int c = 0; c < collider.Length; c++)
        {
            Vector2 cEdgeNormal = NormalBetween(collider[c], collider[(c + 1) % collider.Length]);
            ProjectionLine pointProjected = ProjectPoint(point, radius, cEdgeNormal);
            ProjectionLine colliderProjected = ProjectLine(collider, cEdgeNormal);

            float? responseMagnitude = CollisionResponseAcrossLine(pointProjected, colliderProjected);
            if (responseMagnitude != null && Mathf.Abs((float)responseMagnitude) < Mathf.Abs((float)minResponseMagnitude))
            {
                minResponseMagnitude = (float)responseMagnitude;
                responseNormal = cEdgeNormal;
            }
        }

        return responseNormal * minResponseMagnitude;
    }

    public static Vector2 SATResponse(Vector2 point, Vector2[] collider, float radius) {
        float minResponseMagnitude = float.MaxValue;
        Vector2 responseNormal = Vector2.zero;

        for (int i = 0; i < collider.Length; i++) {
            Vector2 edgeNormal = NormalBetween(collider[i], collider[(i + 1) % collider.Length]);
            ProjectionLine pointProjection = ProjectPoint(point, radius, edgeNormal);
            ProjectionLine colliderProjection = ProjectLine(collider, edgeNormal);

            float? responseMagnitude = CollisionResponseAcrossLine(pointProjection, colliderProjection);

            // If not overlap return Vector2.zero as response
            if (!(pointProjection.start <= colliderProjection.end && pointProjection.end >= colliderProjection.start)) {
                return Vector2.zero;
            }
            // If candidate to collide annotate the min magnitude
            else {
                if (Mathf.Abs((float)responseMagnitude) < Mathf.Abs((float)minResponseMagnitude)) {
                    minResponseMagnitude = (float) responseMagnitude;
                    responseNormal = edgeNormal;
                }
            }
        }

        // If loop ends and not found false then overlaps, return response found
        return responseNormal * minResponseMagnitude;
    }
}
