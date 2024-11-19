using System.Runtime.InteropServices;
using UnityEngine;

namespace _2D {

    public class SimulationManager2D : MonoBehaviour {

        public static SimulationManager2D Instance { get; private set; }

        [Header("Particle Settings")]
        public int numParticles;
        public int paddedNumParticles;
        [Min(0.0f)] public float particleRadius;

        [Header("Collider Settings")]
        public int numColliders;

        [Header("Time Settings")]
        [Range(0.0001f, 1 / 50f)] public float deltaTime;

        [Header("References")]
        [SerializeField] FluidSpawnerManager2D fluidSpawner;
        [SerializeField] FluidColliderManager2D fluidColliderManager;
        [SerializeField] FluidUpdater2D fluidUpdater;
        [SerializeField] FluidRenderer2D fluidRenderer;

        // Compute buffers
        public ComputeBuffer positionsBuffer { get; private set; }
        public ComputeBuffer predictedPosBuffer { get; private set; }
        public ComputeBuffer velocitiesBuffer { get; private set; }
        public ComputeBuffer densitiesBuffer { get; private set; }
        public ComputeBuffer pressuresBuffer { get; private set; }
        public ComputeBuffer sortedSpatialHashedIndicesBuffer { get; private set; }
        public ComputeBuffer lookupHashIndicesBuffer { get; private set; }
        public ComputeBuffer collidersLookupsBuffer { get; private set; }
        public ComputeBuffer collidersVerticesBuffer { get; private set; }
        public ComputeBuffer collidersNormalsBuffer { get; private set; }

        [HideInInspector] public FluidData fluidInitialData;
        [HideInInspector] public ColliderLookup[] collidersLookups; 
        [HideInInspector] public Vector2[] collidersVertices;
        [HideInInspector] public Vector2[] collidersNormals;


        // Private constructor to avoid instantiation
        private SimulationManager2D() { }

        void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(this);
            }
            else {
                Instance = this;
            }
        }

        void Start() {
            fluidInitialData = fluidSpawner.SpawnFluid();
            (collidersLookups, collidersVertices, collidersNormals) = fluidColliderManager.GetColliderData();
            numParticles = fluidInitialData.positions.Length;
            paddedNumParticles = fluidInitialData.sortedSpatialHashedIndices.Length;
            numColliders = collidersLookups.Length;
            Time.fixedDeltaTime = deltaTime;

            InstantiateComputeBuffers();
            FillComputeBuffers();

            fluidRenderer.Init();
            fluidUpdater.Init();
        }

        void InstantiateComputeBuffers() {
            positionsBuffer = new ComputeBuffer(numParticles, sizeof(float) * 2);
            predictedPosBuffer = new ComputeBuffer(numParticles, sizeof(float) * 2);
            velocitiesBuffer = new ComputeBuffer(numParticles, sizeof(float) * 2);
            densitiesBuffer = new ComputeBuffer(numParticles, sizeof(float));
            pressuresBuffer = new ComputeBuffer(numParticles, sizeof(float));
            
            sortedSpatialHashedIndicesBuffer = new ComputeBuffer(paddedNumParticles, sizeof(int) * 2);
            lookupHashIndicesBuffer = new ComputeBuffer(numParticles * 2, sizeof(int) * 2);
            
            collidersLookupsBuffer = new ComputeBuffer(collidersLookups.Length, Marshal.SizeOf(typeof(ColliderLookup)));
            collidersVerticesBuffer = new ComputeBuffer(collidersVertices.Length, sizeof(float) * 2);
            collidersNormalsBuffer = new ComputeBuffer(collidersNormals.Length, sizeof(float) * 2);
        }

        void FillComputeBuffers() {
            positionsBuffer.SetData(fluidInitialData.positions);
            predictedPosBuffer.SetData(fluidInitialData.positions);
            velocitiesBuffer.SetData(fluidInitialData.velocities);
            densitiesBuffer.SetData(fluidInitialData.densities);
            pressuresBuffer.SetData(fluidInitialData.pressures);
            sortedSpatialHashedIndicesBuffer.SetData(fluidInitialData.sortedSpatialHashedIndices);
            lookupHashIndicesBuffer.SetData(fluidInitialData.lookupHashIndices);
            collidersLookupsBuffer.SetData(collidersLookups);
            collidersVerticesBuffer.SetData(collidersVertices);
            collidersNormalsBuffer.SetData(collidersNormals);
        }

        void FixedUpdate() {
            StepSimulation();
        }

        void StepSimulation() {
            fluidUpdater.UpdateFluidState();
        }

        void Update() {
            fluidRenderer.RenderFluid();
        }

        private void OnValidate() {
            fluidUpdater.OnValidate();
            fluidRenderer.OnValidate();
        }

        void OnDestroy() {
            positionsBuffer.Release();
            predictedPosBuffer.Release();
            velocitiesBuffer.Release();
            densitiesBuffer.Release();
            pressuresBuffer.Release();

            sortedSpatialHashedIndicesBuffer.Release();
            lookupHashIndicesBuffer.Release();

            collidersLookupsBuffer.Release();
            collidersVerticesBuffer.Release();
            collidersNormalsBuffer.Release();
        }
    }
    
}