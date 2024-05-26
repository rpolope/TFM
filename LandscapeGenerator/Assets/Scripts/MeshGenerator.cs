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
        public NativeArray<Color> Colors;
        [NativeDisableParallelForRestriction]
        public NativeArray<int> Triangles;
        public TerrainParameters TerrainParameters;
        [NativeDisableParallelForRestriction]
        public MapData MapData;
        public float2 Center;
        public int Resolution;
        public int FacesCount;
        public float Scale;
        public int LODScale;

        public void Execute(int index)
        {
            int x = index % Resolution;
            int z = index / Resolution;
            
            float offset = (Resolution - 1) * 0.5f;
            float xPos = LODScale * (x - offset) * Scale;
            float zPos = LODScale * (z - offset) * Scale;

            // float noiseValue = GenerateNoiseValue(Center + LODScale * new float2(x, z));
            float noiseValue = MapData.HeightMap[index];
            Colors[index] = MapData.ColorMap[Mathf.Clamp(Mathf.Abs(Mathf.RoundToInt(noiseValue * 100)), 0, 99)];
            float height = Scale * noiseValue * TerrainParameters.meshParameters.heightScale;
            
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

        private float GenerateNoiseValue(float2 samplePos)
        {
            float ridgedFactor = TerrainParameters.noiseParameters.ridgeness;
            float noiseValue = Noise.GetNoiseValue(samplePos, TerrainParameters.noiseParameters);
            // float noiseValue = (1 - ridgedFactor) * NoiseGenerator.GetNoiseValue(samplePos, TerrainParameters.noiseParameters);
            // noiseValue += ridgedFactor * NoiseGenerator.GetFractalRidgeNoise(samplePos, TerrainParameters.noiseParameters);
            //
            // if (ridgedFactor > 0.01f)
            //     noiseValue = Mathf.Pow(noiseValue, TerrainParameters.noiseParameters.ridgeRoughness);
            //
            noiseValue = noiseValue < TerrainParameters.meshParameters.waterLevel ? 
                TerrainParameters.meshParameters.waterLevel :  noiseValue;

            return noiseValue;
        }
    }

    public static JobHandle ScheduleMeshGenerationJob(TerrainParameters terrainParameters, int resolution, float scale, float2 center, MapData mapData, ref MeshData meshData)
    {
        var generateMeshJob = new GenerateMeshJob
        {
            Vertices = meshData.Vertices,
            UVs = meshData.UVs,
            Triangles = meshData.Triangles,
            Colors = meshData.Colors,
            MapData = mapData,
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
    public NativeArray<Color> Colors;

    public readonly int LODScale;

    public MeshData(int resolution, int lod) {
        LODScale = (1 << lod);
        resolution = (resolution - 1) / LODScale + 1;
        
        Vertices = new NativeArray<Vector3>(resolution * resolution, Allocator.Persistent);
        Triangles = new NativeArray<int>((resolution - 1) * (resolution - 1) * 6, Allocator.Persistent);
        UVs = new NativeArray<float2>(resolution * resolution, Allocator.Persistent);
        Colors = new NativeArray<Color>(resolution * resolution, Allocator.Persistent);
    }

    public Mesh CreateMesh() {
        Mesh mesh = new Mesh();
        int[] trianglesArray = new int[Triangles.Length];
        Triangles.CopyTo(trianglesArray);
        
        mesh.SetVertices(Vertices);
        mesh.SetTriangles(trianglesArray, 0);
        mesh.SetUVs(0, UVs);
        mesh.SetColors(Colors);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        
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