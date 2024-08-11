using UnityEngine;

public static class SpatialHashingUtils {

    // Neighbour offsets
    public static Vector2[] offsets = new Vector2[] {
        new Vector2(-1, -1),
        new Vector2(-1,  0),
        new Vector2(-1,  1),
        new Vector2( 0, -1),
        new Vector2( 0,  0),
        new Vector2( 0,  1),
        new Vector2( 1, -1),
        new Vector2( 1,  0),
        new Vector2( 1,  1)
    };

    // Hashing primes
    const uint p1 = 73856093;
    const uint p2 = 19349663;
    const uint p3 = 83492791;

    public static Vector2 GetGridPosition(Vector2 position, float supportRadius) {
        return new Vector2(Mathf.Floor(position.x / supportRadius), Mathf.Floor(position.y / supportRadius));
    }

    public static float ComputeGridHash(Vector2 gridPosition) {
        uint[] pos = new uint[2] { (uint)gridPosition.x, (uint)gridPosition.y };
        return pos[0] * p1 ^ pos[1] * p2;
    }

    public static float GetKey(uint hash, uint hashSize) {
        return hash % hashSize;
    }

    // hashLookupIndices: list that has for each hash the first appearance of a particle with that hash in spatialHashIndices
    // spatialHashIndices: list sorted by hash with each particle of that hash, contains (index, hash)
    // _Positions: list with particle positions, not sorted

    public static int GetFirstAppearanceIndex(int[] hashLookupIndices, int hash) {
        return hashLookupIndices[hash];
    }

    public static bool MatchesCurrentHash(int hash, int targetHash) => hash == targetHash;

}
