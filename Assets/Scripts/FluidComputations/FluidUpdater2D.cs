using UnityEngine;

public class FluidUpdater2D : MonoBehaviour {

    [Header("Physical Settings")]
    [SerializeField] float particleMass;
    [SerializeField] float gravity = -9.81f;
    [SerializeField, Range(0f, 1f)] float collisionDamping = 0.95f;

    [Header("Density Calculation Settings")]
    [SerializeField, Min(0f)] float smoothingLength;

    [Header("Bounding Box Settings")]
    public Vector2 boundsCentre = Vector2.zero;
    public Vector2 boundsSize = Vector2.one;
    [SerializeField] bool drawBounds = true;

    [Header("Neighbor search Settings")]
    [SerializeField] bool drawGrid = true;

    // References to compute shaders
    [Header("References")]
    [SerializeField] ComputeShader computeShader;

    // Compute kernel IDs
    const int integratePositionKernel = 0;
    const int applyExternalForcesKernel = 1;
    const int handleCollisionsKernel = 2;
    const int calculateDensitiesKernel = 3;
    const int computeSpatialHashesKernel = 4;
    const int buildSpatialHashLookupKernel = 5;

    // Private fields
    SimulationManager2D manager;
    bool needsUpdate = true;


    public void Init() {
        manager = SimulationManager2D.Instance;
        SetBuffers();
        UpdateSettings();
    }

    void SetBuffers() {
        GraphicsHelper.SetBufferKernels(computeShader, "_Positions", manager.positionsBuffer, integratePositionKernel, handleCollisionsKernel, calculateDensitiesKernel, computeSpatialHashesKernel);
        GraphicsHelper.SetBufferKernels(computeShader, "_Velocities", manager.velocitiesBuffer, applyExternalForcesKernel, integratePositionKernel, handleCollisionsKernel);
        GraphicsHelper.SetBufferKernels(computeShader, "_Densities", manager.densitiesBuffer, calculateDensitiesKernel);
        GraphicsHelper.SetBufferKernels(computeShader, "_SortedSpatialHashedIndices", manager.sortedSpatialHashedIndicesBuffer, calculateDensitiesKernel, computeSpatialHashesKernel, buildSpatialHashLookupKernel);
        GraphicsHelper.SetBufferKernels(computeShader, "_LookupHashIndices", manager.lookupHashIndicesBuffer, calculateDensitiesKernel, computeSpatialHashesKernel, buildSpatialHashLookupKernel);
    }

    void UpdateSettings() {
        if (needsUpdate) {
            Time.fixedDeltaTime = manager.deltaTime;
            computeShader.SetFloat("_deltaTime", manager.deltaTime);
            computeShader.SetInt("_numParticles", manager.numParticles);

            computeShader.SetFloat("_particleMass", particleMass);
            computeShader.SetFloat("_gravity", gravity);
            computeShader.SetFloat("_collisionDamping", collisionDamping);

            computeShader.SetFloat("_smoothingLength", smoothingLength);

            computeShader.SetVector("_boundsCentre", boundsCentre);
            computeShader.SetVector("_boundsSize", boundsSize);

            needsUpdate = false;
        }
    }

    public void UpdateFluidState() {
        int groups = GraphicsHelper.ComputeThreadGroups1D(manager.numParticles);
        UpdateSettings();

        computeShader.Dispatch(computeSpatialHashesKernel, groups, 1, 1);
        manager.sortedSpatialHashedIndicesBuffer.GetData(manager.fluidInitialData.sortedSpatialHashedIndices);
        System.Array.Sort(manager.fluidInitialData.sortedSpatialHashedIndices, (i1, i2) => (int)(((uint)i1[1] % (uint)(2 * manager.numParticles)) - ((uint)i2[1] % (uint)(2 * manager.numParticles))));
        manager.sortedSpatialHashedIndicesBuffer.SetData(manager.fluidInitialData.sortedSpatialHashedIndices);
        computeShader.Dispatch(buildSpatialHashLookupKernel, groups, 1, 1);

        computeShader.Dispatch(calculateDensitiesKernel, groups, 1, 1);
        computeShader.Dispatch(applyExternalForcesKernel, groups, 1, 1);
        computeShader.Dispatch(integratePositionKernel, groups, 1, 1);
        computeShader.Dispatch(handleCollisionsKernel, groups, 1, 1);
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
            Gizmos.matrix = matrix;
        }

        if (drawGrid & Camera.main.orthographic) {
            float camHeight = Camera.main.orthographicSize * 2;
            float camWidth = camHeight * Camera.main.aspect;
            int numLinesX = Mathf.CeilToInt((camWidth + 1) / smoothingLength) + 1;
            int numLinesY = Mathf.CeilToInt((camHeight + 1) / smoothingLength) + 1;
            float maxX = numLinesX / 2 * smoothingLength;
            float maxY = numLinesY / 2 * smoothingLength;

            Gizmos.color = new Color(0.25f, 0.25f, 0.25f, 0.2f);

            for (float i = -numLinesX / 2; i <= numLinesX / 2; i++) {
                Gizmos.DrawLine(new Vector2(i * smoothingLength, -maxY), new Vector2(i * smoothingLength, maxY));
            }

            for (float i = -numLinesY / 2; i <= numLinesY / 2; i++) {
                Gizmos.DrawLine(new Vector2(-maxX, i * smoothingLength), new Vector2(maxX, i * smoothingLength));
            }
        }
    }
}
