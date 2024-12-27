#define BLOCK_SIZE_AXIS 4

// Structs

struct Sample {
    float3 position;
    float value;
};


// Shader properties (Common data)
int3 _samplesPerAxis;
float _isoLevel;

// Structured buffers (Per instance data)
RWStructuredBuffer<Sample> _Samples;


// Helper functions

bool IsVectorOutOfRange(int3 value, int3 max) {
    return any(value >= max);
}

int IndexFromCoords(int x, int y, int z) {
    // TODO: check performance with z_curve
    return x + y * _samplesPerAxis.x + z * _samplesPerAxis.x * _samplesPerAxis.y;
}