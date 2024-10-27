using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestPoint : MonoBehaviour
{

    public float radius;
    public Vector2 velocity;

    private void OnDrawGizmos() {
        Gizmos.DrawWireSphere(transform.position, radius);
        Gizmos.DrawLine(transform.position, transform.position + new Vector3(velocity.x, velocity.y, 0));
    }
}
