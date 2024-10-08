using UnityEngine;

public class FluidRenderer2D : MonoBehaviour {

    [Header("Display Settings")]
    [SerializeField, Min(3)] int numSides = 10;
    [SerializeField, Min(0.0f)] float displaySize;
    [SerializeField] Color particleColor;
    [SerializeField, Range(0f, 1f)] float blendFactor = 1.0f;

    [Header("References")]
    [SerializeField] Mesh particleMesh;
    [SerializeField] Material particleMaterial;

    // Private fields
    SimulationManager2D manager;
    ComputeBuffer argsBuffer;
    Bounds bounds;
    bool needsUpdate = true;


    public void Init() {
        manager = SimulationManager2D.Instance;
        SetBuffers();
        UpdateSettings();
    }

    void SetBuffers() {
        particleMaterial.SetBuffer("Positions", manager.positionsBuffer);
        particleMaterial.SetBuffer("Velocities", manager.velocitiesBuffer);
    }

    void UpdateSettings() {
        if (needsUpdate) {
            bounds = new Bounds(transform.position, Vector3.one * 20000);
            particleMesh = FluidMeshGenerator2D.GenerateMesh(numSides);
            argsBuffer?.Release();
            argsBuffer = GraphicsHelper.CreateArgsBuffer(particleMesh, manager.numParticles);

            // Shader properties
            particleMaterial.SetFloat("_DisplaySize", displaySize);
            particleMaterial.SetColor("_Color", particleColor);
            particleMaterial.SetFloat("_BlendFactor", blendFactor);

            needsUpdate = false;
        }
    }

    public void RenderFluid() {
        UpdateSettings();
        RenderIndirect();
    }

    void RenderIndirect() {
        Graphics.DrawMeshInstancedIndirect(particleMesh, 0, particleMaterial, bounds, argsBuffer);
    }

    void OnValidate() {
        needsUpdate = true;
    }

    void OnDestroy() {
        argsBuffer.Release();
    }
}