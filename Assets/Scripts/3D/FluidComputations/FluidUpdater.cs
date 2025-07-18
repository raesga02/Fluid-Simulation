using UnityEngine;

namespace _3D {

    public class FluidUpdater : MonoBehaviour {

        [Header("General Settings")]
        [SerializeField] int blockSize = 32;
        [SerializeField, Min(0f)] float particleMass;
        [SerializeField] public Vector3 gravity = new Vector3(0.0f, -9.81f, 0.0f);

        [Header("Density Calculation Settings")]
        [SerializeField, Min(0.0001f)] float smoothingLength;

        [Header("Pressure Force Settings")]
        [SerializeField, Min(0f)] public float restDensity;
        [SerializeField, Min(0f)] float bulkModulus;

        [Header("Viscosity Force Settings")]
        [SerializeField, Range(0f, 1f)] float dynamicViscosity;

        [Header("Collision Settings")]
        [SerializeField, Range(0f, 1f)] float collisionDamping = 0.95f;

        [Header("Neighbor search Settings")]
        [SerializeField] bool drawGrid = true;

        // References to compute shaders
        [Header("References")]
        [SerializeField] ComputeShader computeShader;

        // Compute kernel IDs
        int applyExternalForcesKernel;
        int computeSpatialHashesKernel;
        int resetHashLookupKernel;
        int bitonicMergeStepKernel;
        int buildSpatialHashLookupKernel;
        int calculateDensitiesKernel;
        int calculatePressuresKernel;
        int applyPressureForceKernel;
        int applyViscosityForceKernel;
        int integratePositionKernel;
        int handleCollisionsKernel;

        // Private fields
        SimulationManager manager;
        [HideInInspector] public bool needsUpdate = true;


        public void Init() {
            manager = SimulationManager.Instance;
            ComputeKernelsIdxs();
            SetBuffers();
            UpdateSettings();

            // Resolve initial collisions between spawners and colliders
            int groups = GraphicsHelper.ComputeThreadGroups1D(manager.numParticles, blockSize);
            computeShader.Dispatch(handleCollisionsKernel, groups, 1, 1);
        }

        void SetBuffers() {
            GraphicsHelper.SetBufferKernels(computeShader, "_Positions", manager.positionsBuffer, integratePositionKernel, applyExternalForcesKernel, handleCollisionsKernel);
            GraphicsHelper.SetBufferKernels(computeShader, "_PredictedPositions", manager.predictedPosBuffer, applyExternalForcesKernel, calculateDensitiesKernel, computeSpatialHashesKernel, applyPressureForceKernel, applyViscosityForceKernel);
            GraphicsHelper.SetBufferKernels(computeShader, "_Velocities", manager.velocitiesBuffer, applyExternalForcesKernel, integratePositionKernel, handleCollisionsKernel, applyPressureForceKernel, applyViscosityForceKernel);
            GraphicsHelper.SetBufferKernels(computeShader, "_Densities", manager.densitiesBuffer, calculateDensitiesKernel, calculatePressuresKernel, applyPressureForceKernel, applyViscosityForceKernel);
            GraphicsHelper.SetBufferKernels(computeShader, "_Pressures", manager.pressuresBuffer, calculatePressuresKernel, applyPressureForceKernel);
            GraphicsHelper.SetBufferKernels(computeShader, "_SortedSpatialHashedIndices", manager.sortedSpatialHashedIndicesBuffer, calculateDensitiesKernel, computeSpatialHashesKernel, buildSpatialHashLookupKernel, bitonicMergeStepKernel, applyPressureForceKernel, applyViscosityForceKernel);
            GraphicsHelper.SetBufferKernels(computeShader, "_LookupHashIndices", manager.lookupHashIndicesBuffer, calculateDensitiesKernel, resetHashLookupKernel, buildSpatialHashLookupKernel, applyPressureForceKernel, applyViscosityForceKernel);
            GraphicsHelper.SetBufferKernels(computeShader, "_CollidersLookup", manager.collidersLookupsBuffer, handleCollisionsKernel);
            GraphicsHelper.SetBufferKernels(computeShader, "_CollidersVertices", manager.collidersVerticesBuffer, handleCollisionsKernel);
            GraphicsHelper.SetBufferKernels(computeShader, "_CollidersCollisionNormals", manager.collidersCollisionNormalsBuffer, handleCollisionsKernel);
        }

        void UpdateSettings() {
            if (needsUpdate) {
                Time.fixedDeltaTime = manager.deltaTime;
                computeShader.SetFloat("_deltaTime", manager.deltaTime * manager.simulationSpeedFactor);
                computeShader.SetInt("_numParticles", manager.numParticles);
                computeShader.SetInt("_paddedNumParticles", manager.paddedNumParticles);
                computeShader.SetInt("_numColliders", manager.numColliders);

                computeShader.SetFloat("_particleMass", particleMass);
                computeShader.SetFloat("_particleRadius", manager.particleRadius);
                computeShader.SetVector("_gravity", gravity);
                computeShader.SetFloat("_collisionDamping", collisionDamping);

                computeShader.SetFloat("_smoothingLength", smoothingLength);

                computeShader.SetFloat("_restDensity", restDensity);
                computeShader.SetFloat("_bulkModulus", bulkModulus);

                computeShader.SetFloat("_dynamicViscosity", dynamicViscosity);

                needsUpdate = false;
            }
        }

        void ComputeKernelsIdxs() {
            applyExternalForcesKernel = computeShader.FindKernel("ApplyExternalForces");
            computeSpatialHashesKernel = computeShader.FindKernel("ComputeSpatialHashes");
            resetHashLookupKernel = computeShader.FindKernel("ResetHashLookup");
            bitonicMergeStepKernel = computeShader.FindKernel("BitonicMergeStep");
            buildSpatialHashLookupKernel = computeShader.FindKernel("BuildSpatialHashLookup");
            calculateDensitiesKernel = computeShader.FindKernel("CalculateDensities");
            calculatePressuresKernel = computeShader.FindKernel("CalculatePressures");
            applyPressureForceKernel = computeShader.FindKernel("ApplyPressureForce");
            applyViscosityForceKernel = computeShader.FindKernel("ApplyViscosityForce");
            integratePositionKernel = computeShader.FindKernel("IntegratePosition");
            handleCollisionsKernel = computeShader.FindKernel("HandleCollisions");
        }


        public void UpdateFluidState() {
            int groups = GraphicsHelper.ComputeThreadGroups1D(manager.numParticles, blockSize);
            UpdateSettings();
            computeShader.Dispatch(applyExternalForcesKernel, groups, 1, 1);
            PrepareNeighborSearchData(groups);
            computeShader.Dispatch(calculateDensitiesKernel, groups, 1, 1);
            computeShader.Dispatch(calculatePressuresKernel, groups, 1, 1);
            computeShader.Dispatch(applyPressureForceKernel, groups, 1, 1);
            computeShader.Dispatch(applyViscosityForceKernel, groups, 1, 1);
            computeShader.Dispatch(integratePositionKernel, groups, 1, 1);
            computeShader.Dispatch(handleCollisionsKernel, groups, 1, 1);
        }

        private void PrepareNeighborSearchData(int groups) {
            int groupsResetHashLookup = GraphicsHelper.ComputeThreadGroups1D(manager.numParticles * 2);
            computeShader.Dispatch(computeSpatialHashesKernel, groups, 1, 1);
            computeShader.Dispatch(resetHashLookupKernel, groupsResetHashLookup, 1, 1);
            SortParticleIndicesByKey();
            computeShader.Dispatch(buildSpatialHashLookupKernel, groups, 1, 1);
        }

        void SortParticleIndicesByKey() {
            int groups = GraphicsHelper.ComputeThreadGroups1D(manager.paddedNumParticles, blockSize);
            for (int mergeSize = 2; mergeSize <= manager.paddedNumParticles; mergeSize <<= 1) {
                computeShader.SetInt("_mergeSize", mergeSize);
                for (int compareDist = mergeSize / 2; compareDist > 0; compareDist /= 2) {
                    computeShader.SetInt("_compareDist", compareDist);
                    computeShader.Dispatch(bitonicMergeStepKernel, groups, 1, 1);
                }
            }
        }

        public void OnValidate() {
            // Global editor settings
            if (smoothingLength <= 0.25) { drawGrid = false; }

            needsUpdate = true;
        }

        void OnDrawGizmos() {
            if (drawGrid & Camera.main.orthographic) { DrawSPHGrid(); }
        }

        private void DrawSPHGrid() {
            float camHeight = Camera.main.orthographicSize * 2;
            float camWidth = camHeight * Camera.main.aspect;
            int numLinesX = Mathf.CeilToInt((camWidth + 1) / smoothingLength) + 1;
            int numLinesY = Mathf.CeilToInt((camHeight + 1) / smoothingLength) + 1;
            float maxX = numLinesX / 2 * smoothingLength;
            float maxY = numLinesY / 2 * smoothingLength;

            Gizmos.color = new Color(0.25f, 0.25f, 0.25f, 0.2f);

            for (float i = -numLinesX / 2; i <= numLinesX / 2; i++) {
                Gizmos.DrawLine(new Vector2(i * smoothingLength, -maxY), new Vector2(i * smoothingLength, maxY));
            }
            for (float i = -numLinesY / 2; i <= numLinesY / 2; i++) {
                Gizmos.DrawLine(new Vector2(-maxX, i * smoothingLength), new Vector2(maxX, i * smoothingLength));
            }
        }
    }

}