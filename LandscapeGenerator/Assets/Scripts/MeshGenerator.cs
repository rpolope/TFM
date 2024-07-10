using Jobs;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public static class MeshGenerator
{
    public static JobHandle ScheduleMeshGenerationJob(TerrainParameters terrainParameters, int resolution, int2 coords, MapData mapData, ref MeshData meshData, bool globalUVs = true)
    {
        var generateMeshJob = new MeshGenerationJob()
        {
            Vertices = meshData.Vertices,
            UVs = meshData.UVs,
            Triangles = meshData.Triangles,
            Resolution = resolution,
            ChunkFullResolution = terrainParameters.meshParameters.resolution,
            ChunkCoords = coords,
            FacesCount = meshData.Triangles.Length / 6,
            Scale = terrainParameters.meshParameters.scale,
            MapData = mapData,
            LODScale = meshData.LODScale,
            TerrainParameters = terrainParameters,
            GlobalUVs = globalUVs
        };

        var calculateNormalsJob = new CalculateNormalsJob()
        {
            Triangles = meshData.Triangles,
            Vertices = meshData.Vertices,
            Normals = meshData.Normals
        };

        var normalizeNormalsJob = new NormalizeNormalsJob
        {
            Normals = meshData.Normals
        };
        
        var meshJobHandle = generateMeshJob.Schedule(meshData.Vertices.Length, 3000);
        var normalsJobHandle = calculateNormalsJob.Schedule(meshData.Triangles.Length / 3, 3000, meshJobHandle);
        var jobHandle = normalizeNormalsJob.Schedule(meshData.Vertices.Length,3000, normalsJobHandle);
        
        return jobHandle;
    }

}

public class MeshData {
    public NativeArray<Vector3> Vertices;
    public NativeArray<Vector3> Normals;
    public NativeArray<int> Triangles;
    public NativeArray<float2> UVs;

    public readonly int LODScale;

    public MeshData(int resolution, int lod) {
        LODScale = (1 << lod);
        resolution = (resolution - 1) / LODScale + 1;
        
        Vertices = new NativeArray<Vector3>(resolution * resolution, Allocator.Persistent);
        Normals = new NativeArray<Vector3>(resolution * resolution, Allocator.Persistent);
        Triangles = new NativeArray<int>((resolution - 1) * (resolution - 1) * 6, Allocator.Persistent);
        UVs = new NativeArray<float2>(resolution * resolution, Allocator.Persistent);
    }

    public Mesh CreateMesh() {
        Mesh mesh = new Mesh();
        int[] trianglesArray = new int[Triangles.Length];
        Triangles.CopyTo(trianglesArray);
        
        mesh.SetVertices(Vertices);
        mesh.SetTriangles(trianglesArray, 0);
        mesh.SetUVs(0, UVs);
        mesh.SetNormals(Normals);
        mesh.RecalculateBounds();
        
        Dispose();
        
        return mesh;
    }
    
    private void Dispose()
    {
        Vertices.Dispose();
        Normals.Dispose();
        Triangles.Dispose();
        UVs.Dispose();
    }
}

