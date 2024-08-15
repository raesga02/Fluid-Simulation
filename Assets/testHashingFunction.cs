using Unity.VisualScripting;
using UnityEngine;

public class testHashingFunction : MonoBehaviour {
    [SerializeField] bool doTestOnePointValues = false;
    [SerializeField] bool doTestHashCollisions = false;

    [SerializeField] FluidInitializer2D initializer;
    FluidInitializer2D.FluidData fluidData;
    int numParticles;
    [SerializeField] bool initialized = false;
    [SerializeField] bool randomizer = false;
    [SerializeField] bool doComputeStats = false;

    [SerializeField] Camera cam;
    Vector3 mousePos;

    void Init() {
        fluidData = initializer.InitializeFluid();
        numParticles = fluidData.positions.Length;
        initialized = true;
    }

    // For testOnePointValues
    Vector2Int lastGridPos = Vector2Int.zero;

    // For testHashCollisions
    enum HashingMethod {
        XOR_HASHINT_KEYFROMINT,
        XOR_HASHUINT_KEYFROMUINT,
        SUM_HASHINT_KEYFROMINT
    }
    [SerializeField] HashingMethod hashFunction;

    void OnEnable() {
        initialized = false;
    }

    void OnDrawGizmos() {
        if (doTestOnePointValues) { testOnePointValues(); }
        if (doTestHashCollisions) { testHashCollisions(); }
    }

    int GetKey(Vector2 particlePos) {
        // Get the key
        int key = 0;
        switch (hashFunction) {
            case HashingMethod.XOR_HASHINT_KEYFROMINT:
                key = SpatialHashingUtils.GetKeyFromInt(SpatialHashingUtils.ComputeGridHashInt(SpatialHashingUtils.GetGridPosition(particlePos, 1)), 2 * numParticles);
                break;
            case HashingMethod.XOR_HASHUINT_KEYFROMUINT:
                key = SpatialHashingUtils.GetKeyFromUint(SpatialHashingUtils.ComputeGridHashUint(SpatialHashingUtils.GetGridPosition(particlePos, 1)), (uint)(2 * numParticles));
                break;
            case HashingMethod.SUM_HASHINT_KEYFROMINT:
                key = SpatialHashingUtils.GetKeyFromInt(SpatialHashingUtils.ComputeGridHashIntSum(SpatialHashingUtils.GetGridPosition(particlePos, 1)), 2 * numParticles);
                break;
            default:
                break;
        }

        return key;
    }

    void DrawParticles() {
        int targetKey = -1;

        // Check collision
        for (int i = 0; i < numParticles; i++) {
            Vector2 particlePos = fluidData.positions[i];
            Vector2 mouseWorldPos = cam.ScreenToWorldPoint(mousePos);

            if ((mouseWorldPos - particlePos).magnitude <= 0.15f) {
                targetKey = GetKey(particlePos);
            }
        }

        // Put green all points with the target key
        for (int i = 0; i < numParticles; i++) {
            Vector2 particlePos = fluidData.positions[i];
            int key = GetKey(particlePos);

            Gizmos.color = (key == targetKey && targetKey != -1) ? Color.green : Color.cyan;
            Gizmos.DrawSphere(particlePos, 0.1f);
        }
    }

    int numIterations = 0;
    float accumAverageCollisions = 0f;

    void ComputeHashMethodCollisionStats() {
        // Calculate
        //  - number collisions / num particles == average number of collisions
        //  - average number of different cells with the same hash
        int numCollisions = 0;
        int m = 2 * numParticles;
        for (int i = 0; i < numParticles; i++) {
            Vector2 pos1 = fluidData.positions[i];
            Vector2Int gridPos1 = SpatialHashingUtils.GetGridPosition(pos1, m);
            int key1 = GetKey(pos1);

            for (int j = 0; j < numParticles; j++) {
                Vector2 pos2 = fluidData.positions[j];
                Vector2Int gridPos2 = SpatialHashingUtils.GetGridPosition(pos2, m);
                int key2 = GetKey(pos2);
                if (key1 == key2 && gridPos1 != gridPos2) {
                    numCollisions++;
                }
            }
        }

        numIterations++;
        accumAverageCollisions = (accumAverageCollisions * (numIterations - 1) + numCollisions) / (numIterations);

        Debug.Log("This it num collisions: " + numCollisions);
        Debug.Log("Acc avg num collision: " + accumAverageCollisions);

        // collision = same hash but different grid cell

        // array of gridcell - hash: int[numberOfDifferentGridCells][hashSize]
    }

    void DrawGrid() {
        for (int i = -16; i <= 16; i++) {
            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.2f);
            Gizmos.DrawLine(new Vector2(i, 16), new Vector2(i, -16));
            Gizmos.DrawLine(new Vector2(16, i), new Vector2(-16, i));
        }
    }

    void testHashCollisions() {
        if (!initialized) { Init(); }

        // Find the mouse position
        mousePos = Input.mousePosition;

        DrawGrid();
        DrawParticles();
        if (Application.isPlaying && doComputeStats) {
            ComputeHashMethodCollisionStats();
            if (randomizer) { initialized = false; }
        }
    }

    void testOnePointValues() {
        int numParticles = 100000;
        Vector2 pos = transform.position;
        Vector2Int gridPos = SpatialHashingUtils.GetGridPosition(pos, 1);
        int hashInt = SpatialHashingUtils.ComputeGridHashInt(gridPos);
        uint hashUint = SpatialHashingUtils.ComputeGridHashUint(gridPos);
        int keyFromInt = SpatialHashingUtils.GetKeyFromInt(hashInt, 2 * numParticles);
        int keyFromUint = SpatialHashingUtils.GetKeyFromUint(hashUint, (uint)(2 * numParticles));

        Gizmos.DrawSphere(pos, 0.25f);
        Gizmos.DrawWireCube((Vector2)gridPos + 0.5f * Vector2.one, Vector3.one);

        if (lastGridPos != gridPos) {
            Debug.Log("int: " + hashInt + "  uint: " + hashUint + "  int -> uint: " + (uint)hashInt + "  uint -> int: " + (int)hashUint);
            Debug.Log("key from int: " + keyFromInt + "  key from uint: " + keyFromUint);
        }

        lastGridPos = gridPos;
    }

    void OnDisable() {
        initialized = false;
    }
}
