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
        public int LODScale;


        public void Execute(int index)
        {
            int x = index % Resolution;
            int z = index / Resolution;
            
            float offset = (Resolution - 1) * 0.5f * Scale;
            float xPos = LODScale * (x * Scale - offset);
            float zPos = LODScale * (z * Scale - offset);

            float height = GenerateHeight(Center + LODScale * new float2(x, z));
            
            Vertices[index] = new Vector3(xPos, height, zPos);
        }

        private float GenerateHeight(float2 samplePos)
        {
            float noiseValue = NoiseGenerator.GetNoiseValue(samplePos, TerrainParameters.noiseParameters);
            noiseValue += NoiseGenerator.GetOctavedRidgeNoise(samplePos, TerrainParameters.noiseParameters);

            noiseValue = Mathf.Pow(noiseValue/2f, TerrainParameters.noiseParameters.ridgeRoughness);
            
            noiseValue = noiseValue < TerrainParameters.meshParameters.waterLevel ? 
                TerrainParameters.meshParameters.waterLevel :  noiseValue;

            
            return Scale * noiseValue * TerrainParameters.meshParameters.heightScale;
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

    public static Mesh GenerateTerrainMesh(TerrainParameters terrainParameters, int resolution, float scale, float2 center, int lod)
    {
        return ParallelMeshGeneration(terrainParameters, resolution, center, scale, lod);
    }

    private static Mesh ParallelMeshGeneration(TerrainParameters terrainParameters, int resolution, float2 center, float scale, int lod)
    {
        int lodScale = (1 << lod);
        resolution = (resolution - 1) / lodScale + 1;
        
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
            LODScale = lodScale,
            TerrainParameters = terrainParameters
            
        };
        var generateVerticesHandle = generateVerticesJob.Schedule(vertices.Length, 10000);

        var generateTrianglesJob = new GenerateTrianglesJob
        {
            Triangles = triangles,
            Resolution = resolution
        };
        var generateTrianglesHandle = generateTrianglesJob.Schedule(triangles.Length / 6, 10000);

        var generateUVsJob = new GenerateUVsJob
        {
            UVs = uvs,
            Resolution = resolution,
            Scale = scale
        };
        var generateUVsHandle = generateUVsJob.Schedule(uvs.Length, 10000);

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