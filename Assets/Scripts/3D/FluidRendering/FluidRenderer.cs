using System;
using UnityEngine;

namespace _3D {

    public enum VisualizationMode {
        Particles,
        Surface
    }

    public enum ColoringMode {
        FlatColor,
        VelocityMagnitude,
        DensityDeviation,
    }

    public class FluidRenderer : MonoBehaviour {

        [Header("Display Settings")]
        [SerializeField, Min(0.0f)] float displaySizeMultiplier;
        [SerializeField] float independentDisplaySize = 0.2f;
        [SerializeField] bool independentSizing = false;

        [Header("Coloring Mode Parameters")]
        [SerializeField] VisualizationMode visualizationMode = VisualizationMode.Particles;
        [SerializeField] ColoringMode colorMode;
        [SerializeField] Color flatParticleColor;
        [SerializeField] Gradient colorGradient;
        [SerializeField] Gradient densityColorGradient;
        [SerializeField, Range(0f, 1f)] float blendFactor = 1.0f;
        [SerializeField, Min(0.0f)] float maxDisplayVelocity = 20.0f;
        [SerializeField, Min(0.0f)] float densityDeviationRange = 20.0f;
        [SerializeField] bool useLambertIllumination = false;

        [Header("References")]
        [SerializeField] Mesh particleMesh;
        [SerializeField] Material particleMaterial;
        [SerializeField] Light sceneLight;
        [SerializeField] MarchingCubesDisplay marchingCubesDisplay;

        // Private fields
        SimulationManager manager;
        ComputeBuffer argsBuffer;
        Bounds bounds;
        bool needsUpdate = true;


        public void Init() {
            manager = SimulationManager.Instance;
            SetBuffers();
            UpdateSettings();
            marchingCubesDisplay.Init();
        }

        void SetBuffers() {
            particleMaterial.SetBuffer("Positions", manager.positionsBuffer);
            particleMaterial.SetBuffer("Velocities", manager.velocitiesBuffer);
            particleMaterial.SetBuffer("Densities", manager.densitiesBuffer);
        }

        void UpdateSettings() {
            particleMaterial.SetColor("_LightColor", sceneLight.color * sceneLight.intensity);
            particleMaterial.SetVector("_LightDirection", - sceneLight.transform.forward);

            if (!needsUpdate) { return; }
            
            bounds = new Bounds(transform.position, Vector3.one * 20000);
            // TODO: change with custom mesh
            argsBuffer?.Release();
            argsBuffer = GraphicsHelper.CreateArgsBuffer(particleMesh, manager.numParticles);

            // Shader properties
            particleMaterial.SetFloat("_DisplaySize", independentSizing ? independentDisplaySize : manager.particleRadius * displaySizeMultiplier);
            particleMaterial.SetFloat("_BlendFactor", blendFactor);
            particleMaterial.SetInteger("_ColoringMode", colorMode.GetHashCode());
            particleMaterial.SetInteger("_UseLambertIllumination", useLambertIllumination ? 1 : 0);
            particleMaterial.SetColor("_FlatParticleColor", flatParticleColor);
            particleMaterial.SetFloat("_MaxDisplayVelocity", maxDisplayVelocity);
            particleMaterial.SetFloat("_DensityDeviationRange", densityDeviationRange);
            particleMaterial.SetTexture("_ColorGradientTex", GetTex2DFromGradient(colorGradient));
            particleMaterial.SetTexture("_DensityColorGradientTex", GetTex2DFromGradient(densityColorGradient));
            particleMaterial.SetFloat("_RestDensity", manager.fluidUpdater.restDensity);

            needsUpdate = false;
        }

        public void RenderFluid() {
            UpdateSettings();
            RenderIndirect();
        }

        void RenderIndirect() {
            if (visualizationMode != VisualizationMode.Particles) { return;}
            Graphics.DrawMeshInstancedIndirect(particleMesh, 0, particleMaterial, bounds, argsBuffer);
        }

        Texture2D GetTex2DFromGradient(Gradient colorGradient) {
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

        public void OnValidate() {
            needsUpdate = true;

            marchingCubesDisplay.isActive = visualizationMode == VisualizationMode.Surface;
        }

        void OnDestroy() {
            argsBuffer.Release();
        }
    }
    
}