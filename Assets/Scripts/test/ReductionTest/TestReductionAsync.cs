using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class TestReductionASync : MonoBehaviour {
    public int numParticles = 1024;
    public int blockSize = 64;
    [Range(0.0001f, 0.05f)] public float deltaTime;

    ComputeBuffer inputBuffer;
    ComputeBuffer outputBuffer;
    Vector2[] initialCPU;

    [Header("Time results")]
    public int numUpdates = 0;
    public int numCalls = 0;
    int updatesSkipped = 0;
    public int totalSkipped = 0;
    public float updatesSkippedAvg = 0.0f;
    public int maxUpdatesSkippedPerCall = 0;
    public float percentSkipped = 0.0f;

    
    [Header("Operation result")]
    public float[] outputCPU;
    public float maxElement;
    bool firstIt = true;
    AsyncGPUReadbackRequest request;

    [Header("References")]
    public ComputeShader shader;

    // Start is called before the first frame update
    void Start() {
        firstIt = true;
        outputCPU = null;
        // Initialize buffers
        initialCPU = Shuffle(Enumerable.Range(0, numParticles)
                               .Select(i => new Vector2(i, 0))
                               .ToArray());
        outputCPU = new float[numParticles / blockSize];

        inputBuffer = new ComputeBuffer(numParticles, sizeof(float) * 2);
        outputBuffer = new ComputeBuffer(numParticles / blockSize, sizeof(float));

        // Fill buffer
        inputBuffer.SetData(initialCPU);

        GraphicsHelper.SetBufferKernels(shader, "_InputBuffer", inputBuffer, 0);
        GraphicsHelper.SetBufferKernels(shader, "_OutputBuffer", outputBuffer, 0);
        shader.SetInt("_numParticles", numParticles);
        shader.SetFloat("_deltaTime", 0.0f);


    }

    private void FixedUpdate() {
        numUpdates++;
        
        // Calcular el minimo
        int groups = GraphicsHelper.ComputeThreadGroups1D(numParticles, blockSize);
        shader.Dispatch(0, groups, 1, 1);

        if (firstIt) {
            request = AsyncGPUReadback.Request(outputBuffer);
            numCalls ++;
            firstIt = false;
        }

        if (!request.done) {
            updatesSkipped++;
        }
        else {
            updatesSkippedAvg = (updatesSkippedAvg * (numUpdates - 1) + updatesSkipped) / numUpdates;
            maxUpdatesSkippedPerCall = Mathf.Max(maxUpdatesSkippedPerCall, updatesSkipped);
            totalSkipped += updatesSkipped;
            updatesSkipped = 0;
            request = AsyncGPUReadback.Request(outputBuffer);
            numCalls++;
            percentSkipped = totalSkipped / (float)numUpdates * 100;
        }

        outputBuffer.GetData(outputCPU);
        outputCPU = outputCPU.Select(Mathf.Sqrt).ToArray();
        maxElement = outputCPU.Max();
        shader.SetFloat("_deltaTime", maxElement * Random.value);

    }

    private void OnValidate() {
        Time.fixedDeltaTime = deltaTime;
    }

    private void OnDestroy() {
        inputBuffer?.Release();
        outputBuffer?.Release();
    }

    private Vector2[] Shuffle(Vector2[] array) {
        for (int i = array.Length - 1; i > 0; i--) {
            int j = Random.Range(0, i + 1);
            (array[j], array[i]) = (array[i], array[j]);
        }
        return array;
    }
}