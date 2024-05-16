using System.Linq;
using Unity.Burst;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public static class MeshGenerator
{
    [BurstCompile]
    private struct GenerateMeshJob : IJobParallelFor
    {
        public NativeArray<Vector3> Vertices;
        public NativeArray<float2> UVs;
        [NativeDisableParallelForRestriction]
        public NativeArray<int> Triangles;
        public TerrainParameters TerrainParameters;
        public int Resolution;
        public int FacesCount;
        public float Scale;
        public float2 Center;
        public int LODScale;


        public void Execute(int index)
        {
            int x = index % Resolution;
            int z = index / Resolution;
            
            float offset = (Resolution - 1) * 0.5f;
            float xPos = LODScale * (x - offset) * Scale;
            float zPos = LODScale * (z - offset) * Scale;

            float height = GenerateHeight(Center + LODScale * new float2(x, z));
            
            Vertices[index] = new Vector3((int)xPos, height, (int)zPos);
            UVs[index] = new float2((float)x / (Resolution - 1), (float)z / (Resolution - 1));

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

        private float GenerateHeight(float2 samplePos)
        {
            float ridgedFactor = 0.85f;
            // float noiseValue = NoiseGenerator.GetNoiseValue(samplePos, TerrainParameters.noiseParameters);
            float noiseValue = (1 - ridgedFactor) * NoiseGenerator.GetNoiseValue(samplePos, TerrainParameters.noiseParameters);
            noiseValue += ridgedFactor * NoiseGenerator.GetOctavedRidgeNoise(samplePos, TerrainParameters.noiseParameters);
            
            if (ridgedFactor > 0)
                noiseValue = Mathf.Pow(noiseValue, TerrainParameters.noiseParameters.ridgeRoughness);
            
            noiseValue = noiseValue < TerrainParameters.meshParameters.waterLevel ? 
                TerrainParameters.meshParameters.waterLevel :  noiseValue;

            
            return Scale * noiseValue * TerrainParameters.meshParameters.heightScale;
        }
    }

    public static JobHandle ScheduleMeshGenerationJob(TerrainParameters terrainParameters, int resolution, float scale, float2 center, int lod, ref MeshData meshData)
    {
        var generateMeshJob = new GenerateMeshJob
        {
            Vertices = meshData.Vertices,
            UVs = meshData.UVs,
            Triangles = meshData.Triangles,
            Resolution = resolution,
            FacesCount = meshData.Triangles.Length / 6,
            Center = center,
            Scale = scale,
            LODScale = meshData.LODScale,
            TerrainParameters = terrainParameters
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

    public Mesh CreateMesh() {
        Mesh mesh = new Mesh();
        int[] trianglesArray = new int[Triangles.Length];
        Triangles.CopyTo(trianglesArray);
        
        mesh.SetVertices(Vertices);
        mesh.SetTriangles(trianglesArray, 0);
        mesh.SetUVs(0, UVs);
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

}