using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _3D {

    public class FluidSpawner : MonoBehaviour {
        
        [Header("Spawner Settings")]
        public int numParticles = 1000;
        [SerializeField] Vector3 spawnSize = new Vector3(1.0f, 1.0f, 0.0f);
        [SerializeField] Vector3 initialVelocity = new Vector3(0.0f, 0.0f, 0.0f);
        [SerializeField, Min(0.0f)] float areaMultiplier = 1.0f;

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
            Vector3Int[] spawnPositions = GetSpawnPositions();

            for (int i = 0; i < numParticles; i++) {
                Vector3Int gridPosition = spawnPositions[i];
                Vector3 localPos = Vector3.Scale(gridPosition, computedSpacing) - spawnSize * 0.5f;
                Vector3 wPos = transform.localToWorldMatrix * new Vector4(localPos.x, localPos.y, localPos.z, 1.0f);
                
                // TODO: delete after
                Vector2 randomDir1 = Random.insideUnitCircle;
                Vector2 randomDir2 = Random.insideUnitCircle;

                positions[i] = wPos + positionJitter * Random.value * new Vector3(randomDir1.x, randomDir1.y, 0.0f);
                velocities[i] = initialVelocity + velocityJitter * Random.value * new Vector3(randomDir2.x, randomDir2.y, 0.0f);
                densities[i] = 0.0f;
                pressures[i] = 0.0f;
            }

            return new FluidData { positions = positions, velocities = velocities, densities = densities, pressures = pressures };
        }

        private void UpdateSpawner() {
            spawnSize.x = Mathf.Max(spawnSize.x, 1.0f);
            spawnSize.y = Mathf.Max(spawnSize.y, 1.0f);
            
            int particlesPerAxisX = Mathf.CeilToInt(Mathf.Sqrt(numParticles));
            int particlesPerAxisY = Mathf.CeilToInt(numParticles / (float)particlesPerAxisX);
            
            float spacingX = spawnSize.x / Mathf.Max(1, particlesPerAxisX - 1);
            float spacingY = spawnSize.y / Mathf.Max(1, particlesPerAxisY - 1);

            computedParticlesPerAxis = new Vector3Int(particlesPerAxisX, particlesPerAxisY, 1);
            computedSpacing = new Vector3(spacingX, spacingY, 0.0f);
        }

        Vector3Int[] GetSpawnPositions() {
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

        // TODO: cambiar a volumen
        public float SpawnArea() => areaMultiplier * spawnSize.x * spawnSize.y;

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
    
}