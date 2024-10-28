using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class TestSAT : MonoBehaviour
{

    public Vector2 size;
    public TestPoint point;

    public Vector2[] vertex;
    public bool overlap;
    public float damping = 1f;
    public BounceDirection bounceDir = BounceDirection.INSIDE;

    public float[] responses = new float[4];


    private void OnDrawGizmos() {
        vertex = new Vector2[4] {
            transform.position + new Vector3(  size.x / 2 + 5,   size.y / 2, 0f),
            transform.position + new Vector3(- size.x / 2 - 1,   size.y / 2 - 1, 0f),
            transform.position + new Vector3(- size.x / 2, - size.y / 2, 0f),
            transform.position + new Vector3(  size.x / 2, - size.y / 2, 0f)
        };

        // Get position to move to to resolve
        Vector2 moveVector;
        if (bounceDir == BounceDirection.INSIDE) {
            moveVector = Collision.SATResponseHollow(point.transform.position, vertex, point.radius, ref responses);
        }
        else {
             moveVector = Collision.SATResponse(point.transform.position, vertex, point.radius, ref responses);
        }

        overlap = !moveVector.Equals(Vector2.zero);

        //Debug.Log(responses[0] + " " + responses[1] + " " + responses[2] + " " + responses[3]);

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

    private static float? CollisionResponseAcrossLine(ProjectionLine pointProjection, ProjectionLine colliderProjection) {
        if (pointProjection.start >= colliderProjection.end || pointProjection.end <= colliderProjection.start) {
            return null;
        }

        return colliderProjection.end - pointProjection.start;
    }

    private static float? CollisionResponseAcrossLineHollow(ProjectionLine pointProjection, ProjectionLine colliderProjection) {
        if (pointProjection.start >= colliderProjection.start && pointProjection.end <= colliderProjection.end) {
            return null;
        }

        if (pointProjection.start < colliderProjection.start) {
            return colliderProjection.start - pointProjection.start;
        }

        if (pointProjection.end > colliderProjection.end) {
            return pointProjection.end - colliderProjection.end;
        }

        return null;
    }

    public static Vector2 SATResponse(Vector2 point, Vector2[] collider, float radius, ref float[] responses) {
        float minResponseMagnitude = float.MaxValue;
        Vector2 responseNormal = Vector2.zero;

        for (int i = 0; i < collider.Length; i++) {
            Vector2 edgeNormal = NormalBetween(collider[i], collider[(i + 1) % collider.Length]);
            ProjectionLine pointProjection = ProjectPoint(point, radius, edgeNormal);
            ProjectionLine colliderProjection = ProjectLine(collider, edgeNormal);

            float? responseMagnitude = CollisionResponseAcrossLine(pointProjection, colliderProjection);
            responses[i] = responseMagnitude == null ? 0 : (float) responseMagnitude;

            // If not overlap return Vector2.zero as response
            if (responseMagnitude == null) {
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

    public static Vector2 SATResponseHollow(Vector2 point, Vector2[] collider, float radius, ref float[] responses) {
        float minResponseMagnitude = float.MaxValue;
        Vector2 responseNormal = Vector2.zero;

        for (int i = 0; i < collider.Length; i++) {
            Vector2 edgeNormal = NormalBetween(collider[i], collider[(i + 1) % collider.Length]);
            ProjectionLine pointProjection = ProjectPoint(point, radius, edgeNormal);
            ProjectionLine colliderProjection = ProjectLine(collider, edgeNormal);

            float? responseMagnitude = CollisionResponseAcrossLineHollow(pointProjection, colliderProjection);
            responses[i] = responseMagnitude == null ? 0 : (float) responseMagnitude;

            // If candidate to collide annotate the min magnitude
            if (responseMagnitude != null) {
                if (Mathf.Abs((float)responseMagnitude) < Mathf.Abs((float)minResponseMagnitude)) {
                    minResponseMagnitude = (float) responseMagnitude;
                    responseNormal = edgeNormal;
                }
            }
        }

        // If loop ends and not found false then overlaps, return response found
        return responseNormal * - minResponseMagnitude;
    }
}
