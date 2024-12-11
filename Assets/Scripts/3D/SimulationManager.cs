using System.Runtime.InteropServices;
using UnityEngine;

namespace _3D {

    public class SimulationManager : MonoBehaviour {

        public static SimulationManager Instance { get; private set; }

        [Header("Particle Settings")]
        public int numParticles;
        public int paddedNumParticles;
        [Min(0.0f)] public float particleRadius;

        [Header("Collider Settings")]
        public int numColliders;

        [Header("Time Settings")]
        [Range(0.0001f, 1 / 50f)] public float deltaTime;

        [Header("Simulation Control Settings")]
        [Range(0.1f, 1.5f)] public float simulationSpeedFactor = 1f;
        [SerializeField] bool isPaused = false;

        [Header("References")]
        [SerializeField] FluidSpawnerManager fluidSpawner;
        [SerializeField] FluidColliderManager fluidColliderManager;
        [SerializeField] FluidUpdater fluidUpdater;
        [SerializeField] FluidRenderer fluidRenderer;

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
        public ComputeBuffer collidersCollisionNormalsBuffer { get; private set; }

        [HideInInspector] public FluidData fluidInitialData;
        [HideInInspector] public ColliderLookup[] collidersLookups; 
        [HideInInspector] public Vector3[] collidersVertices;
        [HideInInspector] public Vector3[] collidersCollisionNormals;


        // Private constructor to avoid instantiation
        private SimulationManager() { }

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
            (collidersLookups, collidersVertices, collidersCollisionNormals) = fluidColliderManager.GetColliderData();
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
            positionsBuffer = new ComputeBuffer(numParticles, sizeof(float) * 3);
            predictedPosBuffer = new ComputeBuffer(numParticles, sizeof(float) * 3);
            velocitiesBuffer = new ComputeBuffer(numParticles, sizeof(float) * 3);
            densitiesBuffer = new ComputeBuffer(numParticles, sizeof(float));
            pressuresBuffer = new ComputeBuffer(numParticles, sizeof(float));
            
            sortedSpatialHashedIndicesBuffer = new ComputeBuffer(paddedNumParticles, sizeof(int) * 2);
            lookupHashIndicesBuffer = new ComputeBuffer(numParticles * 2, sizeof(int) * 2);
            
            collidersLookupsBuffer = new ComputeBuffer(collidersLookups.Length, Marshal.SizeOf(typeof(ColliderLookup)));
            collidersVerticesBuffer = new ComputeBuffer(collidersVertices.Length, sizeof(float) * 3);
            collidersCollisionNormalsBuffer = new ComputeBuffer(collidersCollisionNormals.Length, sizeof(float) * 3);
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
            collidersCollisionNormalsBuffer.SetData(collidersCollisionNormals);
        }

        void FixedUpdate() {
            if (!isPaused) {
                StepSimulation();
            }
        }

        void StepSimulation() {
            fluidUpdater.UpdateFluidState();
        }

        void Update() {
            fluidRenderer.RenderFluid();

            HandleUserInput();
        }

        void HandleUserInput() {
            if (Input.GetKeyDown("p")) { isPaused = !isPaused; }

            // Movement
            float movementSpeed = 0.25f;
            if (Input.GetKey("w")) { Camera.main.transform.localPosition += Camera.main.transform.forward * movementSpeed; }
            if (Input.GetKey("s")) { Camera.main.transform.localPosition -= Camera.main.transform.forward * movementSpeed; }
            if (Input.GetKey("d")) { Camera.main.transform.localPosition += Camera.main.transform.right * movementSpeed; }
            if (Input.GetKey("a")) { Camera.main.transform.localPosition -= Camera.main.transform.right * movementSpeed; }
            if (Input.GetKey("space")) { Camera.main.transform.localPosition += Camera.main.transform.up * movementSpeed; }
            if (Input.GetKey("left shift")) { Camera.main.transform.localPosition -= Camera.main.transform.up * movementSpeed; }
        }

        private void OnValidate() {
            fluidUpdater.OnValidate();
            fluidRenderer.OnValidate();
        }

        void OnDestroy() {
            positionsBuffer?.Release();
            predictedPosBuffer?.Release();
            velocitiesBuffer?.Release();
            densitiesBuffer?.Release();
            pressuresBuffer?.Release();

            sortedSpatialHashedIndicesBuffer?.Release();
            lookupHashIndicesBuffer?.Release();

            collidersLookupsBuffer?.Release();
            collidersVerticesBuffer?.Release();
            collidersCollisionNormalsBuffer?.Release();
        }
    }
    
}