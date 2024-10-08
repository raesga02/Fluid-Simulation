using TMPro;
using UnityEngine;

public enum ColoringMode {
    FlatColor,
    VelocityMagnitude
}

public class FluidRenderer2D : MonoBehaviour {

    [Header("Display Settings")]
    [SerializeField, Min(3)] int numSides = 10;
    [SerializeField, Min(0.0f)] float displaySize;
    [SerializeField] ColoringMode colorMode;
    [SerializeField, Range(0f, 1f)] float blendFactor = 1.0f;

    [Header("Coloring Mode Parameters")]
    [SerializeField] Color flatParticleColor;
    [SerializeField] Gradient colorGradient;
    [SerializeField, Min(0.0f)] float maxDisplayVelocity = 20.0f;

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
            particleMaterial.SetFloat("_BlendFactor", blendFactor);
            particleMaterial.SetInteger("_ColoringMode", colorMode.GetHashCode());
            particleMaterial.SetColor("_FlatParticleColor", flatParticleColor);
            particleMaterial.SetFloat("_MaxDisplayVelocity", maxDisplayVelocity);
            particleMaterial.SetTexture("_ColorGradientTex", GetTex2DFromGradient());

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

    Texture2D GetTex2DFromGradient() {
        int resolution = 256;
        Texture2D texture = new Texture2D(resolution, 1, TextureFormat.RGBA32, false);
        texture.wrapMode =  TextureWrapMode.Clamp;

        for (int i = 0; i < resolution; i++) {
            float t = i / (float)(resolution - 1);
            texture.SetPixel(i, 0, colorGradient.Evaluate(t));
        }
        texture.Apply();
        
        return texture;
    }

    void OnValidate() {
        needsUpdate = true;
    }

    void OnDestroy() {
        argsBuffer.Release();
    }
}