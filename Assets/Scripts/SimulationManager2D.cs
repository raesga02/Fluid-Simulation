using Unity.VisualScripting;
using UnityEngine;

public class SimulationManager2D : MonoBehaviour {

    public static SimulationManager2D Instance { get; private set; }

    [Header("Settings")]
    public int numParticles;
    [Range(0.0001f, 1 / 50f)] public float deltaTime;

    [Header("References")]
    [SerializeField] FluidInitializer2D fluidSpawner;
    [SerializeField] FluidUpdater2D fluidUpdater;
    [SerializeField] FluidRenderer2D fluidRenderer;

    // Compute buffers
    public ComputeBuffer positionsBuffer { get; private set; }
    public ComputeBuffer velocitiesBuffer { get; private set; }

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
        Time.fixedDeltaTime = deltaTime;

        // Create the compute buffers
        positionsBuffer = new ComputeBuffer(numParticles, 2 * sizeof(float));
        velocitiesBuffer = new ComputeBuffer(numParticles, 2 * sizeof(float));

        // Set the initial data of the compute buffers
        positionsBuffer.SetData(fluidInitialData.positions);
        velocitiesBuffer.SetData(fluidInitialData.velocities);

        // Initialize other modules
        fluidRenderer.Init();
        fluidUpdater.Init();
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
    }
}
