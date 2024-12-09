using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _3D {

    public class FluidSpawner : MonoBehaviour {
        
        [Header("Spawner Settings")]
        public int numParticles = 1000;
        [SerializeField] Vector3 spawnSize = new Vector3(1.0f, 1.0f, 1.0f);
        [SerializeField] Vector3 initialVelocity = new Vector3(0.0f, 0.0f, 0.0f);
        [SerializeField, Min(0.0f)] float volumeMultiplier = 1.0f;

        [Header("Jitter Settings")]
        [SerializeField] float positionJitter = 1;
        [SerializeField] float velocityJitter = 1;

        [Header("Computed values (Display)")]
        [SerializeField] Vector3Int computedParticlesPerAxis;
        [SerializeField] Vector3 computedSpacing;

        [Header("Display Settings")]
        public bool drawSpawnBounds = true;

        public FluidData Spawn() {
            Vector3[] positions = new Vector3[numParticles];
            Vector3[] velocities = new Vector3[numParticles];
            float[] densities = new float[numParticles];
            float[] pressures = new float[numParticles];

            UpdateSpawner();
            Vector3Int[] gridPositions = GetGridPositions();

            for (int i = 0; i < gridPositions.Length; i++) {
                Vector3Int gridPosition = gridPositions[i];
                Vector3 localPos = Vector3.Scale(gridPosition, computedSpacing) - spawnSize * 0.5f;
                Vector3 worldPos = transform.TransformPoint(localPos);

                positions[i] = worldPos + positionJitter * Random.value * Random.insideUnitSphere;
                velocities[i] = initialVelocity + velocityJitter * Random.value * Random.insideUnitSphere;
                densities[i] = 0.0f;
                pressures[i] = 0.0f;
            }

            return new FluidData { positions = positions, velocities = velocities, densities = densities, pressures = pressures };
        }

        private void UpdateSpawner() {
            spawnSize.x = Mathf.Max(spawnSize.x, 1.0f);
            spawnSize.y = Mathf.Max(spawnSize.y, 1.0f);
            spawnSize.z = Mathf.Max(spawnSize.z, 1.0f);
            
            ComputeSpawnerDistribution();
        }

        void ComputeSpawnerDistribution() {
            float spawnVolume = spawnSize.x * spawnSize.y * spawnSize.z;
            float spawnRootVolume = Mathf.Pow(spawnVolume, 1.0f / 3.0f);
            Vector3 axisScaleFactor = spawnSize * (1.0f / spawnRootVolume);

            float idealParticlesPerAxis = Mathf.Pow(numParticles, 1.0f / 3.0f);
            Vector3 rawParticlesPerAxis = idealParticlesPerAxis * axisScaleFactor;

            computedParticlesPerAxis = new Vector3Int(
                Mathf.CeilToInt(rawParticlesPerAxis.x),
                Mathf.CeilToInt(rawParticlesPerAxis.y),
                Mathf.CeilToInt(rawParticlesPerAxis.z)
            );

            // Compute particle spacing
            Vector3 spacingScaleFactor = new Vector3(
                1.0f / Mathf.Max(1, computedParticlesPerAxis.x - 1),
                1.0f / Mathf.Max(1, computedParticlesPerAxis.y - 1),
                1.0f / Mathf.Max(1, computedParticlesPerAxis.z - 1)
            ); 

            computedSpacing = Vector3.Scale(spawnSize, spacingScaleFactor);
        }

        Vector3Int[] GetGridPositions() {
            Vector3Int[] gridPositions = new Vector3Int[computedParticlesPerAxis.x * computedParticlesPerAxis.y * computedParticlesPerAxis.z];
            
            for (int x = 0, i = 0; x < computedParticlesPerAxis.x; x++) {
                for (int y = 0; y < computedParticlesPerAxis.y; y++) {
                    for (int z = 0; z < computedParticlesPerAxis.z; z++, i++) {
                        gridPositions[i] = new Vector3Int(x, y, z);
                    }
                }
            }
            
            return Shuffle(gridPositions).Take(numParticles).ToArray();
        }

        Vector3Int[] Shuffle(Vector3Int[] array) {
            for (int i = array.Length - 1; i > 0; i--) {
                int j = Random.Range(0, i + 1);
                (array[j], array[i]) = (array[i], array[j]);
            }
            return array;
        }

        public float SpawnVolume() => volumeMultiplier * spawnSize.x * spawnSize.y * spawnSize.z;

        void OnDrawGizmos() {
            if (drawSpawnBounds && !Application.isPlaying) {
                var matrix = Gizmos.matrix;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireCube(Vector3.zero, spawnSize);
                Gizmos.matrix = matrix;
            }
        }
    }
    
}