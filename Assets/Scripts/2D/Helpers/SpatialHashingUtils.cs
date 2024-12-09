using UnityEngine;

namespace _2D {

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
        const int p1 = 73856093;
        const int p2 = 19349663;
        const uint p3 = 83492791;

        public static Vector2Int GetGridPosition(Vector2 pos, float supportRadius) {
            return new Vector2Int(Mathf.FloorToInt(pos.x / supportRadius), Mathf.FloorToInt(pos.y / supportRadius));
        }

        public static int ComputeGridHashInt(Vector2Int gridPos) {
            return gridPos.x * p1 ^ gridPos.y * p2;
        }

        public static uint ComputeGridHashUint(Vector2Int gridPos) {
            uint[] pos = new uint[2] { (uint)gridPos.x, (uint)gridPos.y };
            return pos[0] * p1 ^ pos[1] * p2;
        }

        public static int ComputeGridHashIntSum(Vector2Int gridPos) {
            return gridPos.x * p1 + gridPos.y * p2;
        }

        public static int GetKeyFromInt(int hash, int hashSize) {
            return hash % hashSize;
        }

        public static int GetKeyFromUint(uint hash, uint hashSize) {
            return (int)(hash % hashSize);
        }

    }
    
}