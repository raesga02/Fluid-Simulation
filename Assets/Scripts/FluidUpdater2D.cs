using UnityEngine;

public class FluidUpdater2D : MonoBehaviour {

    [Header("Physical Settings")]
    [SerializeField] float particleMass;
    [SerializeField] float gravity;
    // [SerializeField, Range(0.0f, 1.0f)] float collisionDamping = 0.95f;

    // References to compute shaders


    public void UpdateFluidState() {
        SimulationManager2D manager = SimulationManager2D.Instance;
        // Update the fluid properties for 1 step
        FluidInitializer2D.FluidData fluidData = manager.fluidData;

        for (int i = 0; i < manager.numParticles; i++) {
            fluidData.positions[i].x += 0.1f;
        }

        manager.positionsBuffer.SetData(fluidData.positions);
    }
}
