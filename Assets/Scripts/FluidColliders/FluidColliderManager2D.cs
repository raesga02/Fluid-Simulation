using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

[StructLayout(LayoutKind.Sequential)]
public struct ColliderLookup {
    public int startIdx;
    public int numSides;
    public int bounceDirection;
    public AABB aabb;
}

public enum BounceDirection {
    OUTSIDE = 0,
    INSIDE = 1,
}

public class FluidColliderManager2D : MonoBehaviour {
    
    [Header("Fluid Colliders Settings")]
    [SerializeField] bool drawCollidersAABB = false;
    [SerializeField] bool debugUpdateColliders = false;

    [Header("References")]
    public FluidCollider2D[] colliders;

    public (ColliderLookup[] lookups, Vector2[] vertices, Vector2[] normals) GetColliderData() {
        UpdateColliders();

        (Vector2[], Vector2[], AABB)[] collidersData = colliders.Select(collider => collider.GetData()).ToArray();
        Vector2[] collidersVertices = collidersData.SelectMany(dataPair => dataPair.Item1).ToArray();
        Vector2[] collidersNormals = collidersData.SelectMany(dataPair => dataPair.Item2).ToArray();
        ColliderLookup[] collidersLookups = new ColliderLookup[colliders.Length];
        
        int currentIdx = 0;
        for (int i = 0; i < colliders.Length; i++) {
            ColliderLookup lookup = new ColliderLookup { startIdx = currentIdx, 
                                                         numSides = collidersData[i].Item1.Length, 
                                                         bounceDirection = (int)colliders[i].bounceDirection,
                                                         aabb = collidersData[i].Item3 };
            collidersLookups[i] = lookup;
            currentIdx += lookup.numSides;
        }

        return (collidersLookups, collidersVertices, collidersNormals);
    }

    public void UpdateColliders() {
        colliders = GetComponentsInChildren<FluidCollider2D>();
        foreach (FluidCollider2D collider in colliders) {
            collider.drawColliderAABB = drawCollidersAABB;
        }
    }

    private void OnValidate() {
        UpdateColliders();
        if (debugUpdateColliders) { debugUpdateColliders = false; }
    }
}
