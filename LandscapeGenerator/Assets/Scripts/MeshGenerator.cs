using System.Linq;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;

public static class MeshGenerator
{
    private struct GenerateVerticesJob : IJobParallelFor
    {
        public NativeArray<Vector3> Vertices;
        public TerrainParameters TerrainParameters;
        public int Resolution;
        public float Scale;
        public float2 Center;

        public void Execute(int index)
        {
            int x = index % Resolution;
            int z = index / Resolution;
            float xPos = (float)x * Scale - (Resolution - 1) * Scale;
            float zPos = (float)z * Scale - (Resolution - 1) * Scale;
            
            // float height = Mathf.PerlinNoise(xPos, zPos) * HeightScale;
            float noiseValue = NoiseGenerator.GetNoiseValue(Center + new float2(x, z) , TerrainParameters.noiseParameters);
            float height = Scale * noiseValue * TerrainParameters.meshParameters.heightScale;
            height = height < TerrainParameters.meshParameters.waterLevel ? 
                     TerrainParameters.meshParameters.waterLevel :  height;
            
            Vertices[index] = new Vector3(xPos, height, zPos);
        }
    }

    private struct GenerateTrianglesJob : IJobParallelFor
    {
        [ReadOnly] public int Resolution;
        [NativeDisableParallelForRestriction]
        public NativeArray<int> Triangles;

        public void Execute(int index)
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

    private struct GenerateUVsJob : IJobParallelFor
    {
        public NativeArray<float2> UVs;
        public int Resolution;
        public float Scale;

        public void Execute(int index)
        {
            int x = index % Resolution;
            int z = index / Resolution;
            UVs[index] = new float2((float)x / (Resolution - 1) * Scale, (float)z / (Resolution - 1) * Scale);
        }
    }

    public static Mesh GenerateTerrainMesh(TerrainParameters terrainParameters, int resolution, float scale, float2 center)
    {
        return ParallelMeshGeneration(terrainParameters, resolution, center, scale, terrainParameters.meshParameters.heightScale);
    }

    private static Mesh ParallelMeshGeneration(TerrainParameters terrainParameters, int resolution, float2 center, float scale, float heightScale)
    {
        Mesh mesh = new Mesh();
        NativeArray<Vector3> vertices = new NativeArray<Vector3>(resolution * resolution, Allocator.TempJob);
        NativeArray<int> triangles = new NativeArray<int>((resolution - 1) * (resolution - 1) * 6, Allocator.TempJob);
        NativeArray<float2> uvs = new NativeArray<float2>(resolution * resolution, Allocator.TempJob);

        var generateVerticesJob = new GenerateVerticesJob
        {
            Vertices = vertices,
            Resolution = resolution,
            Center = center,
            Scale = scale,
            TerrainParameters = terrainParameters
        };
        var generateVerticesHandle = generateVerticesJob.Schedule(vertices.Length, 64);

        var generateTrianglesJob = new GenerateTrianglesJob
        {
            Triangles = triangles,
            Resolution = resolution
        };
        var generateTrianglesHandle = generateTrianglesJob.Schedule(triangles.Length / 6, 64);

        var generateUVsJob = new GenerateUVsJob
        {
            UVs = uvs,
            Resolution = resolution,
            Scale = scale
        };
        var generateUVsHandle = generateUVsJob.Schedule(uvs.Length, 64);

        JobHandle.CombineDependencies(generateVerticesHandle, generateTrianglesHandle, generateUVsHandle).Complete();

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles.ToList(), 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        vertices.Dispose();
        triangles.Dispose();
        uvs.Dispose();

        return mesh;
    }
}