using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Jobs
{
    [BurstCompile]
    public struct MeshGenerationJob : IJobParallelFor
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
}