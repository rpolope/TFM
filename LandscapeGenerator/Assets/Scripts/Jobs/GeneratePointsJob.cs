using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Jobs
{
    [BurstCompile]
    public struct GeneratePointsJob : IJobParallelFor
    {
        public float Radius;
        public Vector2 SampleRegionSize;
        public int NumSamplesBeforeRejection;
        public NativeArray<Vector2> Points;
        public NativeArray<int> Grid;
        public float CellSize;
        public NativeList<Vector2> SpawnPoints;
        public int Width, Height;

        public void Execute(int index)
        {
            Vector2 sampleRegionHalfSize = SampleRegionSize / 2;
            Vector2 spawnCenter = SpawnPoints[index];
            bool candidateAccepted = false;

            for (int i = 0; i < NumSamplesBeforeRejection; i++)
            {
                float angle = Random.value * Mathf.PI * 2;
                Vector2 dir = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
                Vector2 candidate = spawnCenter + dir * Random.Range(Radius, 2 * Radius);

                if (IsValid(candidate, SampleRegionSize, CellSize, Radius, Points, Grid, Width, Height))
                {
                    Points[index] = candidate;
                    SpawnPoints.Add(candidate);
                    int cellX = Mathf.FloorToInt(candidate.x / CellSize);
                    int cellY = Mathf.FloorToInt(candidate.y / CellSize);
                    Grid[cellX + cellY * Width] = index + 1;
                    candidateAccepted = true;
                    break;
                }
            }

            if (!candidateAccepted)
            {
                SpawnPoints.RemoveAt(index);
            }
        }

        private bool IsValid(Vector2 candidate, Vector2 sampleRegionSize, float cellSize, float radius, NativeArray<Vector2> points, NativeArray<int> grid, int width, int height)
        {
            if (!(candidate.x >= 0) || !(candidate.x < sampleRegionSize.x) || !(candidate.y >= 0) ||
                !(candidate.y < sampleRegionSize.y)) return false;
            
            
            int cellX = Mathf.FloorToInt(candidate.x / cellSize);
            int cellY = Mathf.FloorToInt(candidate.y / cellSize);
            int searchStartX = Mathf.Max(0, cellX - 2);
            int searchEndX = Mathf.Min(cellX + 2, width - 1);
            int searchStartY = Mathf.Max(0, cellY - 2);
            int searchEndY = Mathf.Min(cellY + 2, height - 1);

            for (int x = searchStartX; x <= searchEndX; x++)
            {
                for (int y = searchStartY; y <= searchEndY; y++)
                {
                    int pointIndex = grid[x + y * width] - 1;
                    if (pointIndex != -1)
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
}