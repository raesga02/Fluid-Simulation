using UnityEngine;

namespace _3D {

    public enum ColoringMode {
        FlatColor,
        VelocityMagnitude
    }

    public class FluidRenderer : MonoBehaviour {

        [Header("Display Settings")]
        [SerializeField, Min(3)] int numSides = 10;
        [SerializeField, Min(0.0f)] float displaySizeMultiplier;
        [SerializeField] float independentDisplaySize = 0.2f;
        [SerializeField] bool independentSizing = false;

        [Header("Coloring Mode Parameters")]
        [SerializeField] ColoringMode colorMode;
        [SerializeField] Color flatParticleColor;
        [SerializeField] Gradient colorGradient;
        [SerializeField, Range(0f, 1f)] float blendFactor = 1.0f;
        [SerializeField, Min(0.0f)] float maxDisplayVelocity = 20.0f;

        [Header("References")]
        [SerializeField] Mesh particleMesh;
        [SerializeField] Material particleMaterial;
        
        // Private fields
        SimulationManager manager;
        ComputeBuffer argsBuffer;
        Bounds bounds;
        bool needsUpdate = true;


        public void Init() {
            manager = SimulationManager.Instance;
            SetBuffers();
            UpdateSettings();
        }

        void SetBuffers() {
            particleMaterial.SetBuffer("Positions", manager.positionsBuffer);
            particleMaterial.SetBuffer("Velocities", manager.velocitiesBuffer);
        }

        void UpdateSettings() {
            if (!needsUpdate) { return; }
            
            bounds = new Bounds(transform.position, Vector3.one * 20000);
            // TODO: change with custom mesh
            argsBuffer?.Release();
            argsBuffer = GraphicsHelper.CreateArgsBuffer(particleMesh, manager.numParticles);

            // Shader properties
            particleMaterial.SetFloat("_DisplaySize", independentSizing ? independentDisplaySize : manager.particleRadius * displaySizeMultiplier);
            particleMaterial.SetFloat("_BlendFactor", blendFactor);
            particleMaterial.SetInteger("_ColoringMode", colorMode.GetHashCode());
            particleMaterial.SetColor("_FlatParticleColor", flatParticleColor);
            particleMaterial.SetFloat("_MaxDisplayVelocity", maxDisplayVelocity);
            particleMaterial.SetTexture("_ColorGradientTex", GetTex2DFromGradient());

            needsUpdate = false;
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

        public void OnValidate() {
            needsUpdate = true;
        }

        void OnDestroy() {
            argsBuffer.Release();
        }
    }
    
}