static const int2 neighborOffsets[9] = {
    int2(-1, -1),
    int2(-1,  0),
    int2(-1,  1),
    int2( 0, -1),
    int2( 0,  0),
    int2( 0,  1),
    int2( 1, -1),
    int2( 1,  0),
    int2( 1,  1)
};

// Hashing primes
static const int p1 = 73856093;
static const int p2 = 19349663;

int2 GetCellPos(float2 pos, float supportRadius) {
    return floor(pos / supportRadius);
}

int ComputeHash(int2 gridPos) {
    return (gridPos.x * p1) + (gridPos.y * p2);
}

int GetKey(int hash, int hashSize) {
    return (uint)hash % hashSize;
}