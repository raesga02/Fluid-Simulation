#define NEIGHBOURHOOD_SIZE 27

static const int3 neighbourOffsets[NEIGHBOURHOOD_SIZE] = {
    int3(-1, -1, -1),
    int3(-1,  0, -1),
    int3(-1,  1, -1),
    int3( 0, -1, -1),
    int3( 0,  0, -1),
    int3( 0,  1, -1),
    int3( 1, -1, -1),
    int3( 1,  0, -1),
    int3( 1,  1, -1),
    int3(-1, -1,  0),
    int3(-1,  0,  0),
    int3(-1,  1,  0),
    int3( 0, -1,  0),
    int3( 0,  0,  0),
    int3( 0,  1,  0),
    int3( 1, -1,  0),
    int3( 1,  0,  0),
    int3( 1,  1,  0),
    int3(-1, -1,  1),
    int3(-1,  0,  1),
    int3(-1,  1,  1),
    int3( 0, -1,  1),
    int3( 0,  0,  1),
    int3( 0,  1,  1),
    int3( 1, -1,  1),
    int3( 1,  0,  1),
    int3( 1,  1,  1)
};

// Hashing primes
static const int3 p = int3(73856093, 19349663, 83492791);

static const int INT_MAX = 0x7FFFFFFF;

int3 GetCellPos(float3 pos, float supportRadius) {
    return floor(pos / supportRadius);
}

int ComputeHash(int3 gridPos) {
    return dot(gridPos, p);
}

int GetKey(int hash, int hashSize) {
    return hash == INT_MAX ? INT_MAX : (uint)hash % hashSize;
}

int CompareKeyFromHash(int hash1, int hash2, int hashSize) {
    return GetKey(hash1, hashSize) - GetKey(hash2, hashSize);
}