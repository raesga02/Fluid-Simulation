using System.Linq;
using UnityEngine;

namespace _3D {

    public struct FluidData {
        public Vector3[] positions;
        public Vector3[] velocities;
        public float[] densities;
        public float[] pressures;
        public Vector2Int[] sortedSpatialHashedIndices;
        public Vector2Int[] lookupHashIndices;
    }

    public class FluidSpawnerManager : MonoBehaviour {

        [Header("Fluid Spawning Settings")]
        [SerializeField] int numParticles = 1000;
        [SerializeField] bool debugUpdateSpawners = false;

        [Header("References")]
        public FluidSpawner[] spawners;


        public FluidData SpawnFluid() {
            UpdateSpawners();

            FluidData[] spawnersData = spawners.Select(spawner => spawner.Spawn()).ToArray();

            Vector3[] positions = FlattenField(spawnersData, f => f.positions);
            Vector3[] velocities = FlattenField(spawnersData, f => f.velocities);
            float[] densities = FlattenField(spawnersData, f => f.densities);
            float[] pressures = FlattenField(spawnersData, f => f.pressures);

            Vector2Int[] sortedSpatialHashedIndices = Enumerable.Repeat(new Vector2Int(0, int.MaxValue), NextPowerOf2(numParticles)).ToArray();
            Vector2Int[] lookupHashIndices = Enumerable.Repeat(new Vector2Int(0, 0), numParticles * 2).ToArray();
            
            return new FluidData() { 
                positions = positions, velocities = velocities, densities = densities, pressures = pressures, 
                sortedSpatialHashedIndices = sortedSpatialHashedIndices, lookupHashIndices = lookupHashIndices 
            };
        }

        public void UpdateSpawners() {
            spawners = GetComponentsInChildren<FluidSpawner>();
            DistributeParticles();
        }

        void DistributeParticles() {
            float[] volumes = new float[spawners.Length];
            float totalVolume = 0.0f;
            int maxVolumeIdx = 0;
            int totalParticlesAssigned = 0;
            
            if (spawners.Length == 0) { return; }

            for (int i = 0; i < spawners.Length; i++) {
                float spawnVolume = spawners[i].SpawnVolume();
                volumes[i] = spawnVolume;
                totalVolume += spawnVolume;
                if (i == 0 || volumes[i] > volumes[maxVolumeIdx]) { maxVolumeIdx = i; }
            }

            for (int i = 0; i < spawners.Length; i++) {
                int particlesToAssign = (int) (numParticles * volumes[i] / totalVolume);
                spawners[i].numParticles = particlesToAssign;
                totalParticlesAssigned += particlesToAssign;
            }

            // Assign remaining particles to the max volume spawner
            spawners[maxVolumeIdx].numParticles =  spawners[maxVolumeIdx].numParticles + (numParticles - totalParticlesAssigned);
        }

        T[] FlattenField<T>(FluidData[] data, System.Func<FluidData, T[]> selector) => data.SelectMany(selector).ToArray();

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

        private void OnValidate() {
            UpdateSpawners();
            if (debugUpdateSpawners) { debugUpdateSpawners = false; }
        }
    }
    
}