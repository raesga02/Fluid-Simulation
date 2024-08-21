using Unity.VisualScripting;
using UnityEngine;

public class SimulationManager2D : MonoBehaviour {

    public static SimulationManager2D Instance { get; private set; }

    [Header("Settings")]
    public int numParticles;
    public int paddedNumParticles;
    [Range(0.0001f, 1 / 50f)] public float deltaTime;

    [Header("References")]
    [SerializeField] FluidInitializer2D fluidSpawner;
    [SerializeField] FluidUpdater2D fluidUpdater;
    [SerializeField] FluidRenderer2D fluidRenderer;

    // Compute buffers
    public ComputeBuffer positionsBuffer { get; private set; }
    public ComputeBuffer velocitiesBuffer { get; private set; }
    public ComputeBuffer densitiesBuffer { get; private set; }
    public ComputeBuffer sortedSpatialHashedIndicesBuffer { get; private set; }
    public ComputeBuffer lookupHashIndicesBuffer { get; private set; }
    public ComputeBuffer sortingDirectionsBuffer { get; private set; }

    public FluidInitializer2D.FluidData fluidInitialData;


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
        fluidInitialData = fluidSpawner.InitializeFluid();
        numParticles = fluidInitialData.positions.Length;
        paddedNumParticles = fluidInitialData.sortedSpatialHashedIndices.Length;
        Time.fixedDeltaTime = deltaTime;

        InstantiateComputeBuffers();
        FillComputeBuffers();

        fluidRenderer.Init();
        fluidUpdater.Init();
    }

    void InstantiateComputeBuffers() {
        positionsBuffer = new ComputeBuffer(numParticles, 2 * sizeof(float));
        velocitiesBuffer = new ComputeBuffer(numParticles, 2 * sizeof(float));
        densitiesBuffer = new ComputeBuffer(numParticles, 1 * sizeof(float));
        sortedSpatialHashedIndicesBuffer = new ComputeBuffer(paddedNumParticles, 2 * sizeof(int));
        lookupHashIndicesBuffer = new ComputeBuffer(2 * numParticles, 2 * sizeof(int));
        sortingDirectionsBuffer = new ComputeBuffer(numParticles / 2, 1 * sizeof(bool));
    }

    void FillComputeBuffers() {
        positionsBuffer.SetData(fluidInitialData.positions);
        velocitiesBuffer.SetData(fluidInitialData.velocities);
        densitiesBuffer.SetData(fluidInitialData.densities);
        sortedSpatialHashedIndicesBuffer.SetData(fluidInitialData.sortedSpatialHashedIndices);
        lookupHashIndicesBuffer.SetData(fluidInitialData.lookupHashIndices);
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

    void OnDestroy() {
        positionsBuffer.Release();
        velocitiesBuffer.Release();
        densitiesBuffer.Release();
        sortedSpatialHashedIndicesBuffer.Release();
        lookupHashIndicesBuffer.Release();
        sortingDirectionsBuffer.Release();
    }
}
