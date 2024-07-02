using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public static class MeshGenerator
{
    [BurstCompile]
    private struct GenerateMeshJob : IJobParallelFor
    {
        public NativeArray<Vector3> Vertices;
        public NativeArray<float2> UVs;
        [NativeDisableParallelForRestriction]
        public NativeArray<int> Triangles;
        [NativeDisableParallelForRestriction]
        public TerrainParameters TerrainParameters;
        public int Resolution;
        public float2 ChunkCoords;
        public int FacesCount;
        public float Scale;
        [ReadOnly]
        public MapData MapData;
        public int LODScale;
        public int ChunkFullResolution;

        public void Execute(int index)
        {
            int x = index % Resolution;
            int z = index / Resolution;
            
            float offset = (Resolution - 1) * 0.5f;
            float xPos = LODScale * (x - offset) * Scale;
            float zPos = LODScale * (z - offset) * Scale;
            
            var mapIndex = LODScale * (x + z * ChunkFullResolution);
            float height = MapData.HeightMap[mapIndex] * TerrainParameters.meshParameters.heightScale;
            
            Vertices[index] = new Vector3((int)xPos, height, (int)zPos);
            UVs[index] = new float2(
                (ChunkFullResolution * ChunkCoords.x + x * LODScale) / (LandscapeManager.MapWidth * ChunkFullResolution),
                (ChunkFullResolution * ChunkCoords.y + z * LODScale) / (LandscapeManager.MapHeight * ChunkFullResolution)
            );         
            
            if (index < FacesCount)
            {
                int row = index / (Resolution - 1);
                int col = index % (Resolution - 1);
                int vertexIndex = row * Resolution + col;

                int baseIndex = index * 6;

                Triangles[baseIndex + 0] = vertexIndex;
                Triangles[baseIndex + 1] = vertexIndex + Resolution;
                Triangles[baseIndex + 2] = vertexIndex + Resolution + 1;
                Triangles[baseIndex + 3] = vertexIndex;
                Triangles[baseIndex + 4] = vertexIndex + Resolution + 1;
                Triangles[baseIndex + 5] = vertexIndex + 1;
            }
        }
    }

    public static JobHandle ScheduleMeshGenerationJob(TerrainParameters terrainParameters, int resolution, int2 coords, MapData mapData, ref MeshData meshData)
    {
        var generateMeshJob = new GenerateMeshJob
        {
            Vertices = meshData.Vertices,
            UVs = meshData.UVs,
            Triangles = meshData.Triangles,
            Resolution = resolution,
            ChunkCoords = coords,
            FacesCount = meshData.Triangles.Length / 6,
            Scale = terrainParameters.meshParameters.scale,
            MapData = mapData,
            LODScale = meshData.LODScale,
            TerrainParameters = terrainParameters,
            ChunkFullResolution = terrainParameters.meshParameters.resolution
        };
        var jobHandle = generateMeshJob.Schedule(meshData.Vertices.Length, 3000);
        
        return jobHandle;
    }

}

public class MeshData {
    public NativeArray<Vector3> Vertices;
    public NativeArray<int> Triangles;
    public NativeArray<float2> UVs;

    public readonly int LODScale;

    public MeshData(int resolution, int lod) {
        LODScale = (1 << lod);
        resolution = (resolution - 1) / LODScale + 1;
        
        Vertices = new NativeArray<Vector3>(resolution * resolution, Allocator.Persistent);
        Triangles = new NativeArray<int>((resolution - 1) * (resolution - 1) * 6, Allocator.Persistent);
        UVs = new NativeArray<float2>(resolution * resolution, Allocator.Persistent);
    }
    
    private Vector3[] CalculateNormals()
    {

        NativeArray<Vector3> normals = new NativeArray<Vector3>(Vertices.Length, Allocator.TempJob);
        var computeNormalsJob = new NormalComputeJob()
        {
            Vertices = Vertices,
            Triangles = Triangles,
            Normals = normals
        };
        computeNormalsJob.Schedule(Triangles.Length/3, 3000).Complete();

        // Vector3[] normals = new Vector3[Vertices.Length];
        // for (int i = 0; i < Triangles.Length; i += 3) {
        //     int index0 = Triangles[i];
        //     int index1 = Triangles[i + 1];
        //     int index2 = Triangles[i + 2];
        //
        //     Vector3 v0 = Vertices[index1] - Vertices[index0];
        //     Vector3 v1 = Vertices[index2] - Vertices[index0];
        //     Vector3 normal = Vector3.Cross(v0, v1).normalized;
        //
        //     normals[index0] += normal;
        //     normals[index1] += normal;
        //     normals[index2] += normal;
        // }
        //
        for (int i = 0; i < normals.Length; i++) {
            normals[i] = normals[i].normalized;
        }

        var normalsArray = normals.ToArray();
        normals.Dispose();

        return normalsArray;
    }


    public Mesh CreateMesh() {
        Mesh mesh = new Mesh();
        int[] trianglesArray = new int[Triangles.Length];
        Triangles.CopyTo(trianglesArray);
        
        mesh.SetVertices(Vertices);
        mesh.SetTriangles(trianglesArray, 0);
        mesh.SetUVs(0, UVs);
        // mesh.SetNormals(CalculateNormals());
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        Dispose();
        
        return mesh;
    }
    
    private void Dispose()
    {
        Vertices.Dispose();
        Triangles.Dispose();
        UVs.Dispose();
    }

    [BurstCompile]
    private struct NormalComputeJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] 
        public NativeArray<Vector3> Normals;
        [ReadOnly]
        public NativeArray<int> Triangles;
        [ReadOnly] 
        public NativeArray<Vector3> Vertices;
        public void Execute(int threadIndex)
        {
            int index = threadIndex * 3;
            int index0 = Triangles[index];
            int index1 = Triangles[index + 1];
            int index2 = Triangles[index + 2];

            Vector3 v0 = Vertices[index1] - Vertices[index0];
            Vector3 v1 = Vertices[index2] - Vertices[index0];
            Vector3 normal = Vector3.Cross(v0, v1).normalized;

            Normals[index0] += normal;
            Normals[index1] += normal;
            Normals[index2] += normal;
        }
    }
}

