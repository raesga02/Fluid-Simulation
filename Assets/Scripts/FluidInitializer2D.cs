using UnityEngine;

public class FluidInitializer2D : MonoBehaviour {

    [Header("Spawner Settings")]
    [SerializeField] float spawnSize = 10f;
    [SerializeField] bool drawSpawnBounds = true;

    [Header("Particle Spawning Settings")]
    [SerializeField] int numParticlesPerAxis = 10;
    [SerializeField] Vector2 initialVelocity = Vector2.zero;

    public struct FluidData {
        public Vector2[] positions;
        public Vector2[] velocities;
    }


    public FluidData InitializeFluid() {

        int numParticles = numParticlesPerAxis * numParticlesPerAxis;
        Vector2[] positions = new Vector2[numParticles];
        Vector2[] velocities = new Vector2[numParticles];

        int i = 0;
        for (int x = 0; x < numParticlesPerAxis; x++) {
            for (int y = 0; y < numParticlesPerAxis; y++) {
                float spaceDivision = spawnSize / (numParticlesPerAxis - 1);

                // Particle positions in the local space of the simulation
                float posX = x * spaceDivision - spawnSize * 0.5f;
                float posY = y * spaceDivision - spawnSize * 0.5f;

                float velX = initialVelocity.x;
                float velY = initialVelocity.y;

                // Particle position in world space
                positions[i] = transform.localToWorldMatrix * new Vector4(posX, posY, 0f, 1f);
                velocities[i] = new Vector2(velX, velY);

                i++;
            }
        }

        return new FluidData() { positions = positions, velocities = velocities };
    }

    void OnDrawGizmos() {
        if (drawSpawnBounds && !Application.isPlaying) {
            var matrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(Vector3.zero, Vector2.one * spawnSize);
            Gizmos.DrawLine(new Vector2(-spawnSize / 2f, 0), new Vector2(spawnSize / 2f, 0));
            Gizmos.DrawLine(new Vector2(0, -spawnSize / 2f), new Vector2(0, spawnSize / 2f));
            Gizmos.matrix = matrix;
        }
    }
}
