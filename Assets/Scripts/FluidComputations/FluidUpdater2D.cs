using UnityEngine;

public class FluidUpdater2D : MonoBehaviour {

    [Header("Physical Settings")]
    [SerializeField] float particleMass;
    [SerializeField] float gravity = -9.81f;

    // References to compute shaders
    [Header("References")]
    [SerializeField] ComputeShader computeShader;

    // Compute kernel IDs
    const int integratePositionKernel = 0;
    const int applyExternalForcesKernel = 1;

    SimulationManager2D manager;


    public void Init() {
        manager = SimulationManager2D.Instance;
        SetBuffers();
        UpdateSettings();
    }

    void SetBuffers() {
        GraphicsHelper.SetBufferKernels(computeShader, "_Positions", manager.positionsBuffer, integratePositionKernel);
        GraphicsHelper.SetBufferKernels(computeShader, "_Velocities", manager.velocitiesBuffer, applyExternalForcesKernel, integratePositionKernel);
    }

    void UpdateSettings() {
        computeShader.SetFloat("_deltaTime", manager.deltaTime);
        computeShader.SetInt("_numParticles", manager.numParticles);
        computeShader.SetFloat("_gravity", gravity);
    }

    public void UpdateFluidState() {
        int groups = Mathf.CeilToInt(manager.numParticles / 64f);
        UpdateSettings();
        computeShader.Dispatch(applyExternalForcesKernel, groups, 1, 1);
        computeShader.Dispatch(integratePositionKernel, groups, 1, 1);
    }
}
