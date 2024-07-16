using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class FluidRenderer2D : MonoBehaviour {

    [Header("Display Settings")]
    [SerializeField] float displaySize;

    [Header("References")]
    [SerializeField] Mesh particleMesh;
    [SerializeField] Material particleMaterial;
    SimulationManager2D manager;

    ComputeBuffer argsBuffer;
    Bounds bounds;

    public void Init() {
        manager = SimulationManager2D.Instance;

        // Set the the position buffer on the material
        particleMaterial.SetBuffer("Positions", manager.positionsBuffer);

        // create the args buffer
        argsBuffer = GraphicsHelper.CreateArgsBuffer(particleMesh, manager.numParticles);

        // initialize the rendering bounds of the fluid
        bounds = new Bounds(transform.position, Vector3.one * 20000);
    }

    public void RenderFluid() {
        RenderIndirect();
    }


    void RenderIndirect() {
        Graphics.DrawMeshInstancedIndirect(particleMesh, 0, particleMaterial, bounds, argsBuffer);
    }

    void OnDestroy() {
        argsBuffer.Release();
    }
}