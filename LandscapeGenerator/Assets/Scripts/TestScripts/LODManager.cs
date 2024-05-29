using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class LODManager
{
    public readonly Mesh[] LODMeshes;
    public const int Resolution = 17;

    private MeshData _meshData;

    public LODManager(int lods)
    {
        LODMeshes = new Mesh[lods];
        InitializeMeshData();
        LODMeshes[0] = _meshData.CreateMesh();
    }

    private void InitializeMeshData()
    {
        var mapData = GenerateMapData(Resolution,
            new NoiseParameters(NoiseType.Perlin),
            new NoiseParameters(NoiseType.Voronoi));
        _meshData = new MeshData(Resolution, 0);
        MeshGenerator.ScheduleMeshGenerationJob(new MeshParameters(0), Resolution, mapData, ref _meshData).Complete();
    }

    private MapData GenerateMapData(int resolution, NoiseParameters heightMapParams, NoiseParameters moistureMapParams)
    {
        float[] noiseMap = MapGenerator.GenerateNoiseMap(resolution * resolution, new float2(), heightMapParams);
        float[] moistureMap = MapGenerator.GenerateNoiseMap(resolution * resolution, new float2(), moistureMapParams);
        var colorMap = new Color[noiseMap.Length];

        return new MapData(noiseMap, colorMap);
    }
    
    public Mesh ChangeMeshLOD(int lod, Mesh mesh)
    {
        var lodChangedMesh = new Mesh
        {
            vertices = mesh.vertices,
            triangles = mesh.triangles,
            normals = mesh.normals,
            uv = mesh.uv
        };

        NativeArray<Vector3> vertices = new NativeArray<Vector3>(mesh.vertices, Allocator.TempJob);
        int lodResolution = (Resolution - 1) / (1 << lod) + 1;
        NativeArray<int> triangles = new NativeArray<int>((lodResolution - 1) * (lodResolution - 1) * 6, Allocator.TempJob);

        LODTrianglesGenerationJob lodJob = new LODTrianglesGenerationJob
        {
            Vertices = vertices,
            Triangles = triangles,
            Resolution = Resolution,
            LOD = lod
        };

        var jobHandle = lodJob.Schedule((lodResolution - 1) * (lodResolution - 1), 64);
        jobHandle.Complete();

        LODMeshes[lod].triangles = triangles.ToArray();

        vertices.Dispose();
        triangles.Dispose();

        return lodChangedMesh;
    }

    public Mesh ChangeMeshLOD(int lod)
    {
        LODMeshes[lod] = new Mesh
        {
            vertices = LODMeshes[0].vertices,
            triangles = LODMeshes[0].triangles,
            normals = LODMeshes[0].normals,
            uv = LODMeshes[0].uv
        };

        NativeArray<Vector3> vertices = new NativeArray<Vector3>(LODMeshes[0].vertices, Allocator.TempJob);
        int lodResolution = (Resolution - 1) / (1 << lod) + 1;
        NativeArray<int> triangles = new NativeArray<int>((lodResolution - 1) * (lodResolution - 1) * 6, Allocator.TempJob);

        LODTrianglesGenerationJob lodJob = new LODTrianglesGenerationJob
        {
            Vertices = vertices,
            Triangles = triangles,
            Resolution = Resolution,
            LOD = lod
        };

        var jobHandle = lodJob.Schedule((lodResolution - 1) * (lodResolution - 1), 64);
        jobHandle.Complete();

        LODMeshes[lod].triangles = triangles.ToArray();

        vertices.Dispose();
        triangles.Dispose();

        return LODMeshes[lod];
    }
}