using UnityEngine;

public class SimulationManager2D : MonoBehaviour {

    public static SimulationManager2D Instance { get; private set; }

    [Header("Settings")]
    public int numParticles;

    [Header("Bounding Box Settings")]
    public Vector2 boundsCentre = Vector2.down;
    public Vector2 boundsSize = Vector2.one;
    public bool drawBounds = true;

    [Header("References")]
    [SerializeField] FluidInitializer2D fluidSpawner;
    [SerializeField] FluidUpdater2D fluidUpdater;
    [SerializeField] FluidRenderer2D fluidRenderer;

    // Compute buffers
    public ComputeBuffer positionsBuffer { get; private set; }


    public FluidInitializer2D.FluidData fluidData;


    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(this);
        }
        else {
            Instance = this;
        }
    }

    void Start() {
        fluidData = fluidSpawner.InitializeFluid();
        numParticles = fluidData.positions.Length;

        // Create the compute buffers
        positionsBuffer = new ComputeBuffer(numParticles, 2 * sizeof(float));

        // Set the initial data of the compute buffers
        positionsBuffer.SetData(fluidData.positions);

        // Initialize other modules
        fluidRenderer.Init();
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
    }

    void OnDrawGizmos() {
        if (drawBounds) {
            var matrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(boundsCentre, Vector2.one * boundsSize);
            Gizmos.matrix = matrix;
        }
    }
}
