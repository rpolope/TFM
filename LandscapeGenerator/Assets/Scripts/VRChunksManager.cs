using System.Collections.Generic;
using UnityEngine;

public class VRChunksManager : MonoBehaviour
{
    public GameObject chunkPrefab;
    public int chunkSize = 10;
    public int chunksPerSide = 10;
    public Transform player;

    private List<GameObject> activeChunks = new List<GameObject>();

    void Start()
    {
        InitializeChunks();
    }

    void Update()
    {
        UpdateChunksLOD();
    }

    private void InitializeChunks()
    {
        for (int x = -chunksPerSide / 2; x < chunksPerSide / 2; x++)
        {
            for (int z = -chunksPerSide / 2; z < chunksPerSide / 2; z++)
            {
                Vector3 position = new Vector3(x * chunkSize, 0, z * chunkSize);
                GameObject chunk = Instantiate(chunkPrefab, position, Quaternion.identity);
                activeChunks.Add(chunk);
            }
        }
    }

    private void UpdateChunksLOD()
    {
        foreach (GameObject chunk in activeChunks)
        {
            float distance = Vector3.Distance(player.position, chunk.transform.position);
            int lodLevel = DetermineLODLevel(distance);
            chunk.GetComponent<Chunk>().SetLOD(lodLevel);
        }
    }

    private static int DetermineLODLevel(float distance)
    {
        if (distance < 20) return 0; // High detail
        if (distance < 50) return 1; // Medium detail
        return 2; // Low detail
    }
}

public class Chunk : MonoBehaviour
{
    public Mesh[] lodMeshes;

    public void SetLOD(int lodLevel)
    {
        GetComponent<MeshFilter>().mesh = lodMeshes[lodLevel];
    }
}