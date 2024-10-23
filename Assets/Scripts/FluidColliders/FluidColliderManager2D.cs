using System.Linq;
using UnityEngine;

public struct ColliderData {
    public Vector2 position;
    public Vector2 size;
    public Matrix4x4 rotation;
    public BounceDirection direction;
}

public enum BounceDirection {
    INSIDE = 1,
    OUTSIDE = -1,
}

public class FluidColliderManager2D : MonoBehaviour {
    
    [Header("Fluid Colliders Settings")]
    [SerializeField] bool debugUpdateColliders = false;

    [Header("References")]
    public FluidCollider2D[] colliders;


    public ColliderData[] GetColliders() {
        UpdateColliders();

        return colliders.Select(collider => collider.GetColliderData()).ToArray();
    }

    public void UpdateColliders() {
        colliders = GetComponentsInChildren<FluidCollider2D>();
    }

    private void OnValidate() {
        UpdateColliders();
        if (debugUpdateColliders) { debugUpdateColliders = false; }
    }
}
