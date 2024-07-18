using UnityEngine;

public static class GraphicsHelper {

    // Compute buffers


    // GPU Indirect Instancing

    public static ComputeBuffer CreateArgsBuffer(Mesh mesh, int numInstances) {

        int subMeshIndex = 0;
        int offset = 0;
        uint[] args = new uint[5];
        args[0] = (uint)mesh.GetIndexCount(subMeshIndex);
        args[1] = (uint)numInstances;
        args[2] = (uint)mesh.GetIndexStart(subMeshIndex);
        args[3] = (uint)mesh.GetBaseVertex(subMeshIndex);
        args[3] = (uint)mesh.GetBaseVertex(subMeshIndex);
        args[4] = (uint)offset;

        ComputeBuffer argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);
        return argsBuffer;
    }


    // Compute shaders

    public static void SetBufferKernels(ComputeShader computeShader, string name, ComputeBuffer buffer, params int[] kernelIndices) {
        for (int i = 0; i < kernelIndices.Length; i++) {
            computeShader.SetBuffer(kernelIndices[i], name, buffer);
        }
    }
}
