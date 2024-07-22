using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Jobs
{
    [BurstCompile]
    public struct GeneratePointsJob : IJob
    {
        public float Radius;
        public Vector2 SampleRegionSize;
        public int NumSamplesBeforeRejection;
        [NativeDisableParallelForRestriction]
        public NativeList<Vector2> Points;
        [NativeDisableParallelForRestriction]
        public NativeArray<int> Grid;
        public float CellSize;
        [NativeDisableParallelForRestriction]
        public NativeList<Vector2> SpawnPoints;
        public int Width, Height;
        public uint Seed;

        public void Execute()
        {
            var random = new Random(Seed);
            while (SpawnPoints.Length > 0)
            {
                int spawnIndex = random.NextInt(SpawnPoints.Length);
                Vector2 spawnCenter = SpawnPoints[spawnIndex];
                bool candidateAccepted = false;

                for (int i = 0; i < NumSamplesBeforeRejection; i++)
                {
                    float angle = random.NextFloat() * Mathf.PI * 2;
                    Vector2 dir = new Vector2(math.sin(angle), math.cos(angle));
                    Vector2 candidate = spawnCenter + dir * random.NextFloat(Radius, 2 * Radius);

                    if (IsValid(candidate, SampleRegionSize, CellSize, Radius, Points, Grid, Width, Height))
                    {
                        Points.Add(candidate);
                        SpawnPoints.Add(candidate);
                        int cellX = Mathf.FloorToInt(candidate.x / CellSize);
                        int cellY = Mathf.FloorToInt(candidate.y / CellSize);
                        if (cellX >= 0 && cellX < Width && cellY >= 0 && cellY < Height)
                        {
                            Grid[cellX + cellY * Width] = Points.Length;
                        }
                        candidateAccepted = true;
                        break;
                    }
                }

                if (!candidateAccepted)
                {
                    SpawnPoints.RemoveAtSwapBack(spawnIndex);
                }
            }
        }

        private static bool IsValid(Vector2 candidate, Vector2 sampleRegionSize, float cellSize, float radius, NativeList<Vector2> points, NativeArray<int> grid, int width, int height)
        {
            if (candidate.x >= 0 && candidate.x < sampleRegionSize.x && candidate.y >= 0 && candidate.y < sampleRegionSize.y)
            {
                int cellX = Mathf.FloorToInt(candidate.x / cellSize);
                int cellY = Mathf.FloorToInt(candidate.y / cellSize);

                if (cellX >= 0 && cellX < width && cellY >= 0 && cellY < height)
                {
                    int searchStartX = Mathf.Max(0, cellX - 2);
                    int searchEndX = Mathf.Min(cellX + 2, width - 1);
                    int searchStartY = Mathf.Max(0, cellY - 2);
                    int searchEndY = Mathf.Min(cellY + 2, height - 1);

                    for (int x = searchStartX; x <= searchEndX; x++)
                    {
                        for (int y = searchStartY; y <= searchEndY; y++)
                        {
                            int pointIndex = grid[x + y * width] - 1;
                            if (pointIndex != -1 && pointIndex < points.Length)
                            {
                                float sqrDst = (candidate - points[pointIndex]).sqrMagnitude;
                                if (sqrDst < radius * radius)
                                {
                                    return false;
                                }
                            }
                        }
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
