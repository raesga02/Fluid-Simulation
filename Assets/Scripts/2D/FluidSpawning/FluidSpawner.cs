using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _2D {

    public class FluidSpawner : MonoBehaviour {
        
        [Header("Spawner Settings")]
        public int numParticles = 1000;
        [SerializeField] Vector2 spawnSize = new Vector2(1.0f, 1.0f);
        [SerializeField] Vector2 initialVelocity = new Vector2(0.0f, 0.0f);
        [SerializeField, Min(0.0f)] float areaMultiplier = 1.0f;

        [Header("Jitter Settings")]
        [SerializeField] float positionJitter = 1;
        [SerializeField] float velocityJitter = 1;

        [Header("Computed values (Display)")]
        [SerializeField] Vector2Int computedParticlesPerAxis;
        [SerializeField] Vector2 computedSpacing;

        [Header("Display Settings")]
        public bool drawSpawnBounds = true;

        public FluidData Spawn() {
            Vector2[] positions = new Vector2[numParticles];
            Vector2[] velocities = new Vector2[numParticles];
            float[] densities = new float[numParticles];
            float[] pressures = new float[numParticles];

            UpdateSpawner();
            Vector2Int[] spawnPositions = GetSpawnPositions();

            for (int i = 0; i < numParticles; i++) {
                Vector2Int gridPosition = spawnPositions[i];
                Vector2 localPos = gridPosition * computedSpacing - spawnSize * 0.5f;
                Vector2 wPos = transform.localToWorldMatrix * new Vector4(localPos.x, localPos.y, 0.0f, 1.0f);

                positions[i] = wPos + positionJitter * Random.value * Random.insideUnitCircle;
                velocities[i] = initialVelocity + velocityJitter * Random.value * Random.insideUnitCircle;
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

            computedParticlesPerAxis = new Vector2Int(particlesPerAxisX, particlesPerAxisY);
            computedSpacing = new Vector2(spacingX, spacingY);
        }

        Vector2Int[] GetSpawnPositions() {
            Vector2Int[] gridPositions = new Vector2Int[computedParticlesPerAxis.x * computedParticlesPerAxis.y];

            for (int x = 0, i = 0; x < computedParticlesPerAxis.x; x++) {
                for (int y = 0; y < computedParticlesPerAxis.y; y++, i++) {
                    gridPositions[i] = new Vector2Int(x, y);
                }
            }
            
            return Shuffle(gridPositions).Take(numParticles).ToArray();
        }

        Vector2Int[] Shuffle(Vector2Int[] array) {
            for (int i = array.Length - 1; i > 0; i--) {
                int j = Random.Range(0, i + 1);
                (array[j], array[i]) = (array[i], array[j]);
            }
            return array;
        }

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