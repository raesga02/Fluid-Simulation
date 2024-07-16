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


    FluidInitializer2D.FluidData fluidData;


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
