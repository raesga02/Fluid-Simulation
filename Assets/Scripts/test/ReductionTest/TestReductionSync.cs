using System.Linq;
using UnityEngine;

public class TestReductionSync : MonoBehaviour {
    public int numParticles = 1024;
    public int blockSize = 64;
    [Range(0.0001f, 0.05f)] public float deltaTime;

    ComputeBuffer inputBuffer;
    ComputeBuffer outputBuffer;
    Vector2[] initialCPU;

    [Header("Time results")]
    public float msDelay = 0f;
    public int numCalls = 0;
    public float delayAvg = 0f;
    
    [Header("Operation result")]
    public float[] outputCPU;
    public float maxElement;

    [Header("References")]
    public ComputeShader shader;

    // Start is called before the first frame update
    void Start() {
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

        numCalls = 0;
    }

    private void FixedUpdate() {
        float initialTime = Time.realtimeSinceStartup;
        float totalTime;

        numCalls++;

        // Calcular el minimo
        int groups = GraphicsHelper.ComputeThreadGroups1D(numParticles, blockSize);
        shader.Dispatch(0, groups, 1, 1);

        outputBuffer.GetData(outputCPU);
        outputCPU = outputCPU.Select(Mathf.Sqrt).ToArray();
        maxElement = outputCPU.Max();
        shader.SetFloat("_deltaTime", maxElement * Random.value);

        totalTime = (Time.realtimeSinceStartup - initialTime) * 1000;

        delayAvg = (delayAvg * (numCalls - 1) + totalTime) / numCalls;
        msDelay = totalTime;
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