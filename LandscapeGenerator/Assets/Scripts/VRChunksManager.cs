using System.Collections.Generic;
using UnityEngine;

public class VRChunkManager : MonoBehaviour
{
    public GameObject chunkPrefab;
    public int chunkSize = 32;
    public int chunksPerSide = 10;
    public Transform player;
    private List<Chunk> chunks = new List<Chunk>();

    void Start()
    {
        InitializeChunks();
    }

    void Update()
    {
        UpdateChunks();
        CombineChunksForBatchRendering();
    }

    void InitializeChunks()
    {
        for (int x = 0; x < chunksPerSide; x++)
        {
            for (int z = 0; z < chunksPerSide; z++)
            {
                Vector3 position = new Vector3(x * chunkSize, 0, z * chunkSize);
                GameObject chunkObject = Instantiate(chunkPrefab, position, Quaternion.identity);
                Chunk chunk = chunkObject.GetComponent<Chunk>();
                chunk.Initialize(chunkSize);
                chunks.Add(chunk);
            }
        }
    }

    void UpdateChunks()
    {
        foreach (Chunk chunk in chunks)
        {
            float distance = Vector3.Distance(player.position, chunk.transform.position);
            chunk.SetLODLevel(distance);
        }
    }

    void CombineChunksForBatchRendering()
    {
        // Agrupa y combina los chunks cercanos para reducir los draw calls
        List<CombineInstance> combineInstances = new List<CombineInstance>();
        foreach (Chunk chunk in chunks)
        {
            CombineInstance combineInstance = new CombineInstance();
            combineInstance.mesh = chunk.Mesh;
            combineInstance.transform = chunk.transform.localToWorldMatrix;
            combineInstances.Add(combineInstance);
        }

        Mesh combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combineInstances.ToArray(), true, true);
        
        // Asigna la malla combinada a un MeshFilter para el renderizado
        MeshFilter combinedMeshFilter = gameObject.GetComponent<MeshFilter>();
        if (combinedMeshFilter == null)
        {
            combinedMeshFilter = gameObject.AddComponent<MeshFilter>();
        }
        combinedMeshFilter.mesh = combinedMesh;
    }
}

public class Chunk : MonoBehaviour
{
    public Mesh Mesh { get; private set; }
    private Mesh[] lodMeshes;

    public void Initialize(int size)
    {
        // Inicializa el chunk con diferentes niveles de detalle
        lodMeshes = new Mesh[3];
        lodMeshes[0] = GenerateMesh(size, 1);   // Alto detalle
        lodMeshes[1] = GenerateMesh(size, 2);   // Detalle medio
        lodMeshes[2] = GenerateMesh(size, 4);   // Bajo detalle
    }

    public void SetLODLevel(float distance)
    {
        if (distance < 20)
        {
            Mesh = lodMeshes[0];
        }
        else if (distance < 50)
        {
            Mesh = lodMeshes[1];
        }
        else
        {
            Mesh = lodMeshes[2];
        }
    }

    private Mesh GenerateMesh(int size, int detail)
    {
        // Genera una malla simple con el nivel de detalle dado
        Mesh mesh = new Mesh();
        int verticesPerSide = size / detail;
        Vector3[] vertices = new Vector3[verticesPerSide * verticesPerSide];
        int[] triangles = new int[(verticesPerSide - 1) * (verticesPerSide - 1) * 6];
        // Rellena vértices y triángulos...
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }
}
