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
    [SerializeField, Range(2, 64)] int samplesPerAxis;
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
        numCubes = (samplesPerAxis - 1) * (samplesPerAxis - 1) * (samplesPerAxis - 1);
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
        verticesInitialData = Enumerable.Repeat(Vector3.zero, maxNumTriangles * 3).ToArray();
        normalsInitialData = Enumerable.Repeat(Vector3.one, maxNumTriangles * 3).ToArray();
        verticesCounterInitialData = new uint[1] { 0 };
        commandBufInitialData = Enumerable.Repeat( new GraphicsBuffer.IndirectDrawArgs() { instanceCount = 1, startInstance = 0, startVertex = 0, vertexCountPerInstance = (uint)maxNumTriangles * 3}, 1).ToArray();
    }

    void InstantiateComputeBuffers() {
        samplesBuffer = new ComputeBuffer(samplesPerAxis * samplesPerAxis * samplesPerAxis, Marshal.SizeOf(typeof(Sample)));
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
        Vector3Int numGroupsSample = GraphicsHelper.ComputeThreadGroups3D(samplesPerAxis, samplesPerAxis, samplesPerAxis, Vector3Int.one * 8);
        computeShader.Dispatch(sampleDensitiesKernel, numGroupsSample.x, numGroupsSample.y, numGroupsSample.z);

        // Reset the vertex counter
        computeShader.Dispatch(resetCounterKernel, 1, 1, 1);

        // March the cubes
        Vector3Int numGroupsMarch = GraphicsHelper.ComputeThreadGroups3D(samplesPerAxis - 1, samplesPerAxis - 1, samplesPerAxis - 1, Vector3Int.one * 8);
        computeShader.Dispatch(marchCubesKernel, numGroupsMarch.x, numGroupsMarch.y, numGroupsMarch.z);

        // Draw the mesh
        computeShader.Dispatch(prepareIndirectArgsKernel, 1, 1, 1);
        Graphics.RenderPrimitivesIndirect(rp, MeshTopology.Triangles, commandBuf, 1);
    }

    void UpdateSettings() {
        if (!needsUpdate) { return; }

        computeShader.SetFloat("_isoLevel", isoLevel);
        computeShader.SetFloat("_samplesPerAxis", samplesPerAxis);
        computeShader.SetInt("_maxNumVertices", maxNumTriangles * 3);

        needsUpdate = false;
    }

    private void OnValidate() {
        needsUpdate = true;   
    }

    private void OnDrawGizmos() {
        if (drawBounds) { DrawBounds(); }
        if (drawSamplePoints && samplesPerAxis < 25) { DrawSamplePoints(); }
    }

    void DrawBounds() {
        Gizmos.color = Color.gray;
        Gizmos.DrawWireCube(transform.position, bounds);
    }

    private void DrawSamplePoints() {
        Gizmos.color = new Color(0.25f, 0.5f, 0.75f, 0.5f);
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
