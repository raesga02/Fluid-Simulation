using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

[StructLayout(LayoutKind.Sequential)]
public struct Sample {
    public Vector3 position;
    public float value;
};

[StructLayout(LayoutKind.Sequential)]
public struct Vertex {
    public Vector3 position;
    public Vector3 normal;
};

public class MarchingCubesTest : MonoBehaviour {

    [Header("Marching Cubes Settings")]
    [SerializeField] Vector3 bounds;
    [SerializeField, Range(2, 15)] int samplesPerAxis;
    [SerializeField] float isoLevel;
    [SerializeField] bool resetPending;

    [Header("Display Settings")]
    [SerializeField] bool drawBounds;
    [SerializeField] bool drawSamplePoints;

    [Header("Array Sizes")]
    [SerializeField, Range(0, 5)] float averageTrianglesPerCube;
    [SerializeField, Range(0f, 1f)] float activeFraction;
    [SerializeField] int numCubes;
    [SerializeField] int maxNumTriangles;
    [SerializeField] int heuristicNumTriangles;

    [Header("References")]
    [SerializeField] Material material;
    [SerializeField] ComputeShader computeShader;

    // Compute buffers
    public ComputeBuffer samplesBuffer { get; private set; }
    public ComputeBuffer verticesBuffer { get; private set; }
    public ComputeBuffer verticesCounterBuffer { get; private set; }
    public ComputeBuffer trianglesBuffer { get; private set; }

    // Initial compute buffers data
    [HideInInspector] public Sample[] samplesInitialData;
    [HideInInspector] public Vertex[] verticesInitialData; 
    [HideInInspector] public uint[] verticesCounterInitialData;
    [HideInInspector] public int[] trianglesInitialData;

    // Compute kernels IDs
    const int sampleDensitiesKernel = 0;
    const int resetCounterKernel = 1;
    const int marchCubesKernel = 2;

    // Private fields
    bool needsUpdate = true;

    // DEBUG
    [Header("DEBUG")]
    public uint verticesAdded;
    public Vector3[] vertPoss;
    public Vector3[] normDir;
    public int[] triangIdx;
    public bool draw = false;


    void Start() {
        Init();
    }   

    void Init() {
        UpdateSettings();

        samplesInitialData = new Sample[samplesPerAxis * samplesPerAxis * samplesPerAxis];
        Vector3 spacing = bounds / (samplesPerAxis - 1);
        Vector3 origin = transform.position - bounds * 0.5f;
        for (int z = 0; z < samplesPerAxis; z++) {
            for (int y = 0; y < samplesPerAxis; y++) {
                for (int x = 0; x < samplesPerAxis; x++) {
                    int sampleIdx = x + y * samplesPerAxis + z * samplesPerAxis * samplesPerAxis;
                    Vector3 samplePos = origin + Vector3.Scale(new Vector3Int(x, y, z), spacing);
                    samplesInitialData[sampleIdx] = new Sample() { position = samplePos, value = 0.0f };
                }
            }
        }
        verticesInitialData = Enumerable.Repeat(new Vertex() { position = Vector3.zero, normal = Vector3.zero }, maxNumTriangles * 3).ToArray();
        verticesCounterInitialData = new uint[1] { 0 };
        trianglesInitialData = Enumerable.Repeat(0, maxNumTriangles * 3).ToArray();

        InstantiateComputeBuffers();
        FillComputeBuffers();
        SetBuffers();
    }

    void ResetDisplay() {
        ReleaseBuffers();
        Init();

        resetPending = false;
    }

    void InstantiateComputeBuffers() {
        samplesBuffer = new ComputeBuffer(samplesPerAxis * samplesPerAxis * samplesPerAxis, Marshal.SizeOf(typeof(Sample)));
        verticesBuffer = new ComputeBuffer(maxNumTriangles * 3, sizeof(float) * 6);
        verticesCounterBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Counter);
        trianglesBuffer = new ComputeBuffer(maxNumTriangles * 3, sizeof(int), ComputeBufferType.Append);
    }

    void FillComputeBuffers() {
        samplesBuffer.SetData(samplesInitialData);
        verticesBuffer.SetData(verticesInitialData);
        verticesCounterBuffer.SetData(verticesCounterInitialData);
        trianglesBuffer.SetData(trianglesInitialData);
    }

    void SetBuffers() {
        GraphicsHelper.SetBufferKernels(computeShader, "_Samples", samplesBuffer, marchCubesKernel, sampleDensitiesKernel);
        GraphicsHelper.SetBufferKernels(computeShader, "_Vertices", verticesBuffer, marchCubesKernel);
        GraphicsHelper.SetBufferKernels(computeShader, "_VerticesCounter", verticesCounterBuffer, marchCubesKernel, resetCounterKernel);
        GraphicsHelper.SetBufferKernels(computeShader, "_Triangles", trianglesBuffer, marchCubesKernel);
    }

    void ReleaseBuffers() {
        samplesBuffer?.Release();
        verticesBuffer?.Release();
        verticesCounterBuffer?.Release();
        trianglesBuffer?.Release();
    }

    void Update() {
        if (resetPending) { ResetDisplay(); }
        UpdateSettings();

        // Sample the densities
        Vector3Int numGroupsSample = GraphicsHelper.ComputeThreadGroups3D(samplesPerAxis, samplesPerAxis, samplesPerAxis, Vector3Int.one * 8);
        computeShader.Dispatch(sampleDensitiesKernel, numGroupsSample.x, numGroupsSample.y, numGroupsSample.z);

        // Reset the counters
        computeShader.Dispatch(resetCounterKernel, 1, 1, 1);
        trianglesBuffer.SetCounterValue(0);

        // March the cubes
        Vector3Int numGroupsMarch = GraphicsHelper.ComputeThreadGroups3D(samplesPerAxis - 1, samplesPerAxis - 1, samplesPerAxis - 1, Vector3Int.one * 8);
        computeShader.Dispatch(marchCubesKernel, numGroupsMarch.x, numGroupsMarch.y, numGroupsMarch.z);

        // Get the number of vertices to draw
        // TODO: change with dispatch kernel that modifies args buffer
        uint[] numVerticesCounter = new uint[1];
        verticesCounterBuffer.GetData(numVerticesCounter);
        verticesAdded = numVerticesCounter[0];

        verticesBuffer.GetData(verticesInitialData);
        vertPoss = verticesInitialData.Select(vertex => vertex.position).ToArray();
        normDir = verticesInitialData.Select(vertex => vertex.normal).ToArray();
        trianglesBuffer.GetData(trianglesInitialData);
        triangIdx = trianglesInitialData;

        // TODO: Draw the mesh
        draw = true;
    }

    void UpdateSettings() {
        if (!needsUpdate) { return; }

        numCubes = (samplesPerAxis - 1) * (samplesPerAxis - 1) * (samplesPerAxis - 1);
        maxNumTriangles = 5 * numCubes;
        heuristicNumTriangles = (int)(averageTrianglesPerCube * activeFraction * numCubes);

        computeShader.SetFloat("_isoLevel", isoLevel);
        computeShader.SetFloat("_samplesPerAxis", samplesPerAxis);
        computeShader.SetInt("_maxNumVertices", maxNumTriangles * 3);

        needsUpdate = false;
    }

    private void OnValidate() {
        needsUpdate = true;
        if (!Application.isPlaying) { UpdateSettings(); }
    }

    private void OnDrawGizmos() {
        if (drawBounds) { DrawBounds(); }
        if (drawSamplePoints) { DrawSamplePoints(); }

        if (draw) {
            for (int i = 0; i < verticesAdded; i+= 3) {
                Vector3[] triangleVertices = vertPoss.Skip(i).Take(3).ToArray();
                Vector3 normal = normDir[i];
                Vector3 avgPos = triangleVertices.Aggregate(Vector3.zero, (acc, v) => acc + v) / 3;
                Gizmos.color = new Color(0.35f, 0.65f, 0.85f);
                Gizmos.DrawLineStrip(triangleVertices, true);
                Gizmos.color = new Color(normal.x, normal.y, normal.z, 1.0f);
                Gizmos.DrawLine(avgPos, avgPos + normal * 0.2f);
            }
        }
    }

    void DrawBounds() {
        Gizmos.color = Color.gray;
        Gizmos.DrawWireCube(transform.position, bounds);
    }

    private void DrawSamplePoints() {
        Gizmos.color = new Color(0.25f, 0.5f, 0.75f);
        Vector3 spacing = bounds / (samplesPerAxis - 1);
        Vector3 origin = transform.position - bounds * 0.5f;
        for (int i = 0; i < samplesPerAxis; i++) {
            for (int j = 0; j < samplesPerAxis; j++) {
                for (int k = 0; k < samplesPerAxis; k++) {
                    Vector3 samplePos = origin + Vector3.Scale(new Vector3Int(i, j, k), spacing);
                    Gizmos.DrawCube(samplePos, Vector3.one * 0.05f);
                }
            }
        }
    }

    private void OnDestroy() {
        ReleaseBuffers();
    }
}
