static const int3 neighborOffsets[9] = {
    int3(-1, -1, 0),
    int3(-1,  0, 0),
    int3(-1,  1, 0),
    int3( 0, -1, 0),
    int3( 0,  0, 0),
    int3( 0,  1, 0),
    int3( 1, -1, 0),
    int3( 1,  0, 0),
    int3( 1,  1, 0)
}; // TODO: change to full 3D neighbourhood

// Hashing primes
static const int p1 = 73856093;
static const int p2 = 19349663;
static const int p3 = 83492791;

static const int INT_MAX = 0x7FFFFFFF;

int3 GetCellPos(float3 pos, float supportRadius) {
    return floor(pos / supportRadius);
}

int ComputeHash(int3 gridPos) {
    return (gridPos.x * p1) + (gridPos.y * p2) + (gridPos.z * 0.0); // TODO: change so it doesnt ignore z
}

int GetKey(int hash, int hashSize) {
    return hash == INT_MAX ? INT_MAX : (uint)hash % hashSize;
}

int CompareKeyFromHash(int hash1, int hash2, int hashSize) {
    return GetKey(hash1, hashSize) - GetKey(hash2, hashSize);
}