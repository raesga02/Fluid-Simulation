using UnityEngine;

public static class SpatialHashingUtils {

    // Neighbour offsets
    public static Vector2[] offsets = new Vector2[] {
        new Vector2(-1, -1),
        new Vector2(-1, 0),
        new Vector2(-1, 1),
        new Vector2(0, -1),
        new Vector2(0, 0),
        new Vector2(0, 1),
        new Vector2(1, -1),
        new Vector2(1, 0),
        new Vector2(1, 1)
    };

    // Hashing primes
    const uint p1 = 73856093;
    const uint p2 = 19349663;
    const uint p3 = 83492791;

    public static Vector2 GetGridPosition(Vector2 position, float supportRadius) {
        return new Vector2(Mathf.Floor(position.x), Mathf.Floor(position.y));
    }

    public static float ComputeGridHash(Vector2 gridPosition) {
        uint[] pos = new uint[2] { (uint)gridPosition.x, (uint)gridPosition.y };
        return pos[0] * p1 ^ pos[1] * p2;
    }

    public static float GetKey(uint hash, uint hashSize) {
        return hash % hashSize;
    }
}
