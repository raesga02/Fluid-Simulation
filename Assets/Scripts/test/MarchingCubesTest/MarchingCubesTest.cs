using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

[StructLayout(LayoutKind.Sequential)]
public struct Sample {
    public Vector3 position;
    public float value;
};

public class MarchingCubesTest : MonoBehaviour {

    [Header("Marching Cubes Settings")]
    [SerializeField] Vector3 bounds;
    [SerializeField] Vector3Int samplesPerAxis;
    [SerializeField] float isoLevel;
    [SerializeField] bool resetPending;

    [Header("Display Settings")]
    [SerializeField] bool drawBounds;
    [SerializeField] bool drawSamplePoints;
    [SerializeField, Range(0.02f, 0.75f)] float sampleScale;

    [Header("Array Sizes")]
    [SerializeField, Range(0, 5)] float averageTrianglesPerCube;
    [SerializeField, Range(0f, 1f)] float activeFraction;
    [SerializeField] int numCubes;
    [SerializeField] int numSamples;
    [SerializeField] int maxNumTriangles;
    [SerializeField] int heuristicNumTriangles;

    [Header("References")]
    [SerializeField] Material material;
    [SerializeField] ComputeShader computeShader;
    [SerializeField] Light sceneLight;

    // Compute buffers
    public ComputeBuffer samplesBuffer { get; private set; }
    public GraphicsBuffer verticesBuffer { get; private set; }
    public GraphicsBuffer normalsBuffer { get; private set; }
    public ComputeBuffer verticesCounterBuffer { get; private set; }
    public GraphicsBuffer commandBuf { get; private set; }

    // Initial compute buffers data
    [HideInInspector] public Sample[] samplesInitialData;
    [HideInInspector] public Vector3[] verticesInitialData; 
    [HideInInspector] public Vector3[] normalsInitialData; 
    [HideInInspector] public uint[] verticesCounterInitialData;
    [HideInInspector] public GraphicsBuffer.IndirectDrawArgs[] commandBufInitialData;

    // Compute kernels IDs
    const int sampleDensitiesKernel = 0;
    const int resetCounterKernel = 1;
    const int marchCubesKernel = 2;
    const int prepareIndirectArgsKernel = 3;

    // Private fields
    bool needsUpdate = true;
    RenderParams rp;


    void Start() {
        Init();
    }   

    void Init() {
        numCubes = (samplesPerAxis.x - 1) * (samplesPerAxis.y - 1) * (samplesPerAxis.z - 1);
        numSamples = samplesPerAxis.x * samplesPerAxis.y * samplesPerAxis.z;
        maxNumTriangles = 5 * numCubes;
        heuristicNumTriangles = (int)(averageTrianglesPerCube * activeFraction * numCubes);

        UpdateSettings();
        
        GenerateInitialData();
        InstantiateComputeBuffers();
        FillComputeBuffers();
        SetBuffers();
    }

    void ResetDisplay() {
        ReleaseBuffers();
        Init();

        resetPending = false;
    }

    void GenerateInitialData() {
        samplesInitialData = new Sample[numSamples];
        Vector3 spacing = new Vector3(bounds.x / (samplesPerAxis.x - 1), bounds.y / (samplesPerAxis.y - 1), bounds.z / (samplesPerAxis.z - 1));
        Vector3 origin = transform.position - bounds * 0.5f;
        for (int z = 0; z < samplesPerAxis.z; z++) {
            for (int y = 0; y < samplesPerAxis.y; y++) {
                for (int x = 0; x < samplesPerAxis.x; x++) {
                    int sampleIdx = x + y * samplesPerAxis.x + z * samplesPerAxis.x * samplesPerAxis.y;
                    Vector3 samplePos = origin + Vector3.Scale(new Vector3Int(x, y, z), spacing);
                    samplesInitialData[sampleIdx] = new Sample() { position = samplePos, value = 0.0f };
                }
            }
        }
        verticesInitialData = Enumerable.Repeat(Vector3.zero, maxNumTriangles * 3).ToArray();
        normalsInitialData = Enumerable.Repeat(Vector3.one, maxNumTriangles * 3).ToArray();
        verticesCounterInitialData = new uint[1] { 0 };
        commandBufInitialData = Enumerable.Repeat( new GraphicsBuffer.IndirectDrawArgs() { instanceCount = 1, startInstance = 0, startVertex = 0, vertexCountPerInstance = (uint)maxNumTriangles * 3}, 1).ToArray();
    }

    void InstantiateComputeBuffers() {
        samplesBuffer = new ComputeBuffer(numSamples, Marshal.SizeOf(typeof(Sample)));
        verticesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, maxNumTriangles * 3, sizeof(float) * 3);
        normalsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, maxNumTriangles * 3, sizeof(float) * 3);
        verticesCounterBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Counter);
        commandBuf = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawArgs.size);
    }

    void FillComputeBuffers() {
        samplesBuffer.SetData(samplesInitialData);
        verticesBuffer.SetData(verticesInitialData);
        normalsBuffer.SetData(normalsInitialData);
        verticesCounterBuffer.SetData(verticesCounterInitialData);
        commandBuf.SetData(commandBufInitialData);
    }

    void SetBuffers() {
        GraphicsHelper.SetBufferKernels(computeShader, "_Samples", samplesBuffer, marchCubesKernel, sampleDensitiesKernel);
        GraphicsHelper.SetBufferKernels(computeShader, "_Vertices", verticesBuffer, marchCubesKernel);
        GraphicsHelper.SetBufferKernels(computeShader, "_Normals", normalsBuffer, marchCubesKernel);
        GraphicsHelper.SetBufferKernels(computeShader, "_VerticesCounter", verticesCounterBuffer, marchCubesKernel, resetCounterKernel, prepareIndirectArgsKernel);
        GraphicsHelper.SetBufferKernels(computeShader, "_IndirectDrawArgs", commandBuf, prepareIndirectArgsKernel);
    
        rp = new RenderParams(material);
        rp.worldBounds = new Bounds(transform.position, bounds * 10);
        rp.matProps = new MaterialPropertyBlock();
        rp.matProps.SetBuffer("_Vertices", verticesBuffer);
        rp.matProps.SetBuffer("_Normals", normalsBuffer);
    }

    void ReleaseBuffers() {
        samplesBuffer?.Release();
        verticesBuffer?.Release();
        normalsBuffer?.Release();
        verticesCounterBuffer?.Release();
        commandBuf?.Release();
    }

    void Update() {
        if (resetPending) { ResetDisplay(); }
        UpdateSettings();

        // Sample the densities
        Vector3Int numGroupsSample = GraphicsHelper.ComputeThreadGroups3D(samplesPerAxis.x, samplesPerAxis.y, samplesPerAxis.z, Vector3Int.one * 8);
        computeShader.Dispatch(sampleDensitiesKernel, numGroupsSample.x, numGroupsSample.y, numGroupsSample.z);

        // Reset the vertex counter
        computeShader.Dispatch(resetCounterKernel, 1, 1, 1);

        // March the cubes
        Vector3Int numGroupsMarch = GraphicsHelper.ComputeThreadGroups3D(samplesPerAxis.x - 1, samplesPerAxis.y - 1, samplesPerAxis.z - 1, Vector3Int.one * 8);
        computeShader.Dispatch(marchCubesKernel, numGroupsMarch.x, numGroupsMarch.y, numGroupsMarch.z);

        // Draw the mesh
        computeShader.Dispatch(prepareIndirectArgsKernel, 1, 1, 1);
        Graphics.RenderPrimitivesIndirect(rp, MeshTopology.Triangles, commandBuf, 1);
    }

    void UpdateSettings() {
        if (!needsUpdate) { return; }

        computeShader.SetFloat("_isoLevel", isoLevel);
        computeShader.SetInts("_samplesPerAxis", samplesPerAxis.x, samplesPerAxis.y, samplesPerAxis.z);
        computeShader.SetInt("_maxNumVertices", maxNumTriangles * 3);

        needsUpdate = false;
    }

    private void OnValidate() {
        needsUpdate = true;   
    }

    private void OnDrawGizmos() {
        if (drawBounds) { DrawBounds(); }
        if (drawSamplePoints && numSamples < 15000) { DrawSamplePoints(); }
    }

    void DrawBounds() {
        Gizmos.color = Color.gray;
        Gizmos.DrawWireCube(transform.position, bounds);
    }

    private void DrawSamplePoints() {
        Gizmos.color = new Color(0.25f, 0.5f, 0.75f, 0.5f);
        Vector3 spacing = new Vector3(bounds.x / (samplesPerAxis.x - 1), bounds.y / (samplesPerAxis.y - 1), bounds.z / (samplesPerAxis.z - 1));
        Vector3 origin = transform.position - bounds * 0.5f;
        for (int z = 0; z < samplesPerAxis.z; z++) {
            for (int y = 0; y < samplesPerAxis.y; y++) {
                for (int x = 0; x < samplesPerAxis.x; x++) {
                    Vector3 samplePos = origin + Vector3.Scale(new Vector3Int(x, y, z), spacing);
                    Gizmos.DrawCube(samplePos, Vector3.one * sampleScale);
                }
            }
        }
    }

    private void OnDestroy() {
        ReleaseBuffers();
    }
}
