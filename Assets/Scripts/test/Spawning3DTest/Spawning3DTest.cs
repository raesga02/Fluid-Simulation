using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour {

    [Min(0)] public int numParticles = 1000;

    public float idealAxisLength = 0;
    public float sizeSqrtCube = 0;
    public Vector3 portionSize; 
    public Vector3Int particlesPerAxis;
    public int paddedNumParticles;

    public Vector3 computedSpacing;

    private void GenerateSpawningPositions() {
        Vector3 spawnSize = transform.localScale;
        float sizeMult = spawnSize.x * spawnSize.y * spawnSize.z;

        idealAxisLength = Mathf.Pow(numParticles, 1.0f / 3.0f);
        sizeSqrtCube = Mathf.Pow(sizeMult, 1.0f / 3.0f);
        portionSize = spawnSize * (1f / sizeSqrtCube);
        Vector3 particlesPerAxisFloat = idealAxisLength * portionSize;
        particlesPerAxis = new Vector3Int(
            Mathf.CeilToInt(particlesPerAxisFloat.x),
            Mathf.CeilToInt(particlesPerAxisFloat.y),
            Mathf.CeilToInt(particlesPerAxisFloat.z)
        );

        paddedNumParticles = particlesPerAxis.x * particlesPerAxis.y * particlesPerAxis.z;
    
        computedSpacing.x = spawnSize.x / Mathf.Max(1, particlesPerAxis.x - 1);
        computedSpacing.y = spawnSize.y / Mathf.Max(1, particlesPerAxis.y - 1);
        computedSpacing.z = spawnSize.z / Mathf.Max(1, particlesPerAxis.z - 1);
    }

    Vector3Int[] GetSpawnPositions() {
        Vector3Int[] gridPositions = new Vector3Int[particlesPerAxis.x * particlesPerAxis.y * particlesPerAxis.z];
        
        for (int x = 0, i = 0; x < particlesPerAxis.x; x++) {
            for (int y = 0; y < particlesPerAxis.y; y++) {
                for (int z = 0; z < particlesPerAxis.z; z++, i++) {
                    gridPositions[i] = new Vector3Int(x, y, z);
                }
            }
        }
        
        return Shuffle(gridPositions).ToArray();
    }

    Vector3Int[] Shuffle(Vector3Int[] array) {
        for (int i = array.Length - 1; i > 0; i--) {
            int j = Random.Range(0, i + 1);
            (array[j], array[i]) = (array[i], array[j]);
        }
        return array;
    }

    private void OnDrawGizmos() {
        GenerateSpawningPositions();
        Vector3Int[] spawnPositions = GetSpawnPositions();

        for (int i = 0; i < spawnPositions.Length; i++) {
            Vector3Int gridPosition = spawnPositions[i];
            Vector3 localPos = Vector3.Scale(gridPosition, computedSpacing) - transform.localScale * 0.5f;
            Vector3 wPos = new Vector4(localPos.x, localPos.y, localPos.z, 1.0f);
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawCube(wPos, Vector3.one);
        }

        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}
