using UnityEngine;

public class FluidInitializer2D : MonoBehaviour {

    [Header("Spawner Settings")]
    [SerializeField] float spawnSize = 10f;
    [SerializeField] bool drawSpawnBounds = true;

    [Header("Particle Spawning Settings")]
    [SerializeField, Min(2)] int numParticlesPerAxis = 10;
    [SerializeField, Min(0f)] float positionJitter = 0.01f;
    [SerializeField] Vector2 initialVelocity = Vector2.zero;
    [SerializeField, Min(0f)] float velocityJitter = 0.01f;

    public struct FluidData {
        public Vector2[] positions;
        public Vector2[] velocities;
        public float[] densities;
        public float[] pressures;
        public Vector2Int[] sortedSpatialHashedIndices;
        public Vector2Int[] lookupHashIndices;
    }

    int NextPowerOf2(int n) {
        n--;
        n |= n >> 1;   // Divide by 2^k for consecutive doublings of k up to 32,
        n |= n >> 2;   // and then or the results.
        n |= n >> 4;
        n |= n >> 8;
        n |= n >> 16;
        n++;
        return n;
    }


    public FluidData InitializeFluid() {

        int numParticles = numParticlesPerAxis * numParticlesPerAxis;
        int paddedNumParticles = NextPowerOf2(numParticles);
        Vector2[] positions = new Vector2[numParticles];
        Vector2[] velocities = new Vector2[numParticles];
        float[] densities = new float[numParticles];
        float[] pressures = new float[numParticles];
        Vector2Int[] sortedSpatialHashedIndices = new Vector2Int[paddedNumParticles];
        Vector2Int[] lookupHashIndices = new Vector2Int[2 * numParticles];

        int i = 0;
        for (int x = 0; x < numParticlesPerAxis; x++) {
            for (int y = 0; y < numParticlesPerAxis; y++) {
                float spaceDivision = spawnSize / (numParticlesPerAxis - 1);

                // Particle positions in the local space of the simulation
                float posX = x * spaceDivision - spawnSize * 0.5f;
                float posY = y * spaceDivision - spawnSize * 0.5f;
                Vector2 wPos = transform.localToWorldMatrix * new Vector4(posX, posY, 0f, 1f);

                float velX = initialVelocity.x;
                float velY = initialVelocity.y;

                positions[i] = wPos + positionJitter * Random.value * Random.insideUnitCircle;
                velocities[i] = new Vector2(velX, velY) + velocityJitter * Random.value * Random.insideUnitCircle;
                sortedSpatialHashedIndices[i] = new Vector2Int(0, 0);
                lookupHashIndices[i] = new Vector2Int(0, 0);
                lookupHashIndices[i + numParticlesPerAxis * numParticlesPerAxis] = new Vector2Int(0, 0);

                i++;
            }
        }

        // Fill the rest of sortedSpatialHashedIndices
        for (; i < paddedNumParticles; i++) {
            sortedSpatialHashedIndices[i] = new Vector2Int(0, int.MaxValue);
        }

        return new FluidData() { positions = positions, velocities = velocities, densities = densities, pressures = pressures, sortedSpatialHashedIndices = sortedSpatialHashedIndices, lookupHashIndices = lookupHashIndices };
    }

    void OnDrawGizmos() {
        if (drawSpawnBounds && !Application.isPlaying) {
            var matrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(Vector3.zero, Vector2.one * spawnSize);
            Gizmos.matrix = matrix;
        }
    }
}
