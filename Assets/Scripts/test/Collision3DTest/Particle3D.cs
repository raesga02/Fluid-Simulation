using UnityEngine;

public class Particle3D : MonoBehaviour
{
    
    [Header("Particle Settings")]
    public float radius;

    void OnDrawGizmos() {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
