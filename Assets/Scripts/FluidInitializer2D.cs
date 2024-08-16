using UnityEngine;

public class FluidInitializer2D : MonoBehaviour {

    [Header("Spawner Settings")]
    [SerializeField] float spawnSize = 10f;
    [SerializeField] bool drawSpawnBounds = true;

    [Header("Particle Spawning Settings")]
    [SerializeField] int numParticlesPerAxis = 10;
    [SerializeField] float positionJitter = 0.01f;
    [SerializeField] Vector2 initialVelocity = Vector2.zero;
    [SerializeField] float velocityJitter = 0.01f;
    [SerializeField] float initialDensity = 0.0f;

    public struct FluidData {
        public Vector2[] positions;
        public Vector2[] velocities;
        public float[] densities;
        public Vector2Int[] sortedSpatialHashedIndices;
        public Vector2Int[] lookupHashIndices;
    }


    public FluidData InitializeFluid() {

        int numParticles = numParticlesPerAxis * numParticlesPerAxis;
        Vector2[] positions = new Vector2[numParticles];
        Vector2[] velocities = new Vector2[numParticles];
        float[] densities = new float[numParticles];
        Vector2Int[] sortedSpatialHashedIndices = new Vector2Int[numParticles];
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

                positions[i] = wPos + positionJitter * Random.insideUnitCircle;
                velocities[i] = new Vector2(velX, velY) + velocityJitter * Random.insideUnitCircle;
                densities[i] = initialDensity;
                sortedSpatialHashedIndices[i] = new Vector2Int(0, 0);
                lookupHashIndices[i] = new Vector2Int(0, 0);
                lookupHashIndices[i + numParticlesPerAxis * numParticlesPerAxis] = new Vector2Int(0, 0);

                i++;
            }
        }

        return new FluidData() { positions = positions, velocities = velocities, densities = densities, sortedSpatialHashedIndices = sortedSpatialHashedIndices, lookupHashIndices = lookupHashIndices };
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
