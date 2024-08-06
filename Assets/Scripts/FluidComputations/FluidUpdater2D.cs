using UnityEngine;

public class FluidUpdater2D : MonoBehaviour {

    [Header("Physical Settings")]
    [SerializeField] float particleMass;
    [SerializeField] float gravity = -9.81f;
    [SerializeField, Range(0f, 1f)] float collisionDamping = 0.95f;

    [Header("Density Calculation Settings")]
    [SerializeField, Min(0f)] float kernelSupportRadius;
    [SerializeField, Min(0f)] float smoothingLength;

    [Header("Bounding Box Settings")]
    public Vector2 boundsCentre = Vector2.zero;
    public Vector2 boundsSize = Vector2.one;
    public bool drawBounds = true;

    // References to compute shaders
    [Header("References")]
    [SerializeField] ComputeShader computeShader;

    // Compute kernel IDs
    const int integratePositionKernel = 0;
    const int applyExternalForcesKernel = 1;
    const int handleCollisionsKernel = 2;
    const int calculateDensitiesKernel = 3;

    // Private fields
    SimulationManager2D manager;
    bool needsUpdate = true;


    public void Init() {
        manager = SimulationManager2D.Instance;
        SetBuffers();
        UpdateSettings();
    }

    void SetBuffers() {
        GraphicsHelper.SetBufferKernels(computeShader, "_Positions", manager.positionsBuffer, integratePositionKernel, handleCollisionsKernel, calculateDensitiesKernel);
        GraphicsHelper.SetBufferKernels(computeShader, "_Velocities", manager.velocitiesBuffer, applyExternalForcesKernel, integratePositionKernel, handleCollisionsKernel);
        GraphicsHelper.SetBufferKernels(computeShader, "_Densities", manager.densitiesBuffer, calculateDensitiesKernel);
    }

    void UpdateSettings() {
        if (needsUpdate) {
            Time.fixedDeltaTime = manager.deltaTime;
            computeShader.SetFloat("_deltaTime", manager.deltaTime);
            computeShader.SetInt("_numParticles", manager.numParticles);
            computeShader.SetFloat("_gravity", gravity);
            computeShader.SetFloat("_collisionDamping", collisionDamping);
            computeShader.SetVector("_boundsCentre", boundsCentre);
            computeShader.SetVector("_boundsSize", boundsSize);
            computeShader.SetFloat("_particleMass", particleMass);
            computeShader.SetFloat("_kernelSupportRadius", kernelSupportRadius);
            computeShader.SetFloat("_smoothingLength", smoothingLength);

            needsUpdate = false;
        }
    }

    public void UpdateFluidState() {
        int groups = Mathf.CeilToInt(manager.numParticles / 64f);
        UpdateSettings();
        computeShader.Dispatch(applyExternalForcesKernel, groups, 1, 1);
        computeShader.Dispatch(integratePositionKernel, groups, 1, 1);
        computeShader.Dispatch(handleCollisionsKernel, groups, 1, 1);
    }

    void DebugPrintParticleEnergy(int particleIndex) {
        Vector2[] pos = new Vector2[particleIndex + 1];
        Vector2[] vel = new Vector2[particleIndex + 1];
        manager.positionsBuffer.GetData(pos, 0, 0, particleIndex + 1);
        manager.velocitiesBuffer.GetData(vel, 0, 0, particleIndex + 1);
        float kineticEnergy = 0.5f * particleMass * vel[particleIndex].magnitude * vel[particleIndex].magnitude;
        float potentialEnergy = particleMass * Mathf.Abs(gravity) * Mathf.Abs(pos[particleIndex].y + boundsSize.y / 2f);
        Debug.Log("Kinetic: " + kineticEnergy + "  Potential: " + potentialEnergy + "  Total: " + (kineticEnergy + potentialEnergy));
    }

    void OnValidate() {
        needsUpdate = true;
    }

    void OnDrawGizmos() {
        if (drawBounds) {
            var matrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(boundsCentre, Vector2.one * boundsSize);
            Gizmos.DrawLine(boundsCentre - new Vector2(0, boundsSize.y / 2f), boundsCentre + new Vector2(0, boundsSize.y / 2f));
            Gizmos.DrawLine(boundsCentre - new Vector2(boundsSize.x / 2f, 0f), boundsCentre + new Vector2(boundsSize.x / 2f, 0f));
            Gizmos.matrix = matrix;
        }
    }
}
