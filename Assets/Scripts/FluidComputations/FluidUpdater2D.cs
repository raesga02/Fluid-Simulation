using UnityEngine;

public class FluidUpdater2D : MonoBehaviour {

    [Header("Physical Settings")]
    [SerializeField] float particleMass;
    [SerializeField] float gravity = -9.81f;
    [SerializeField, Range(0f, 1f)] float collisionDamping = 0.95f;

    [Header("Density Calculation Settings")]
    [SerializeField, Min(0f)] float smoothingLength;
    enum NeighborSearchMode { Naive, SpatialHash };
    [SerializeField] NeighborSearchMode searchMode;


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
            computeShader.SetInt("_searchMode", (int)searchMode);

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

        /*
        // Print the sortedSpatialHashedIndices list
        string toPrint = "sortedSpatialHashedIndices: ";
        for (int i = 0; i < manager.numParticles; i++) {
            toPrint += manager.fluidInitialData.sortedSpatialHashedIndices[i] + " ";
        }
        Debug.Log(toPrint);


        // Get lookup array from gpu
        manager.lookupHashIndicesBuffer.GetData(manager.fluidInitialData.lookupHashIndices);

        toPrint = "lookupHashIndices:          ";
        for (int i = 0; i < 2 * manager.numParticles; i++) {
            toPrint += manager.fluidInitialData.lookupHashIndices[i] + " ";
        }
        Debug.Log(toPrint);
        */


        computeShader.Dispatch(calculateDensitiesKernel, groups, 1, 1);

        /*
        // Print the densities computed
        manager.densitiesBuffer.GetData(manager.fluidInitialData.densities);
        string toPrintDensities = "Densities: (";
        for (int i = 0; i < manager.numParticles; i++) {
            toPrintDensities += manager.fluidInitialData.densities[i] + ", ";
        }
        toPrintDensities += ")";
        Debug.Log(toPrintDensities);
        */

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
            //Gizmos.DrawLine(boundsCentre - new Vector2(0, boundsSize.y / 2f), boundsCentre + new Vector2(0, boundsSize.y / 2f));
            //Gizmos.DrawLine(boundsCentre - new Vector2(boundsSize.x / 2f, 0f), boundsCentre + new Vector2(boundsSize.x / 2f, 0f));
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
