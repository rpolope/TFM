using System.Collections.Generic;
using Jobs;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public static class PoissonDiskSampler
{
    public static List<Vector2> GeneratePoints(float radius, Vector2 sampleRegionSize, int numSamplesBeforeRejection = 10)
    {
        float cellSize = radius / Mathf.Sqrt(2);
        int width = Mathf.CeilToInt(sampleRegionSize.x / cellSize);
        int height = Mathf.CeilToInt(sampleRegionSize.y / cellSize);
        NativeArray<int> grid = new NativeArray<int>(width * height, Allocator.TempJob);
        NativeList<Vector2> points = new NativeList<Vector2>(Allocator.TempJob);
        NativeList<Vector2> spawnPoints = new NativeList<Vector2>(Allocator.TempJob);
        spawnPoints.Add(sampleRegionSize / 2);

        var job = new GeneratePointsJob
        {
            Radius = radius,
            SampleRegionSize = sampleRegionSize,
            NumSamplesBeforeRejection = numSamplesBeforeRejection,
            Points = points,
            Grid = grid,
            CellSize = cellSize,
            SpawnPoints = spawnPoints,
            Width = width,
            Height = height,
            Seed = (uint)UnityEngine.Random.Range(1, 100000)
        };

        job.Schedule().Complete();

        List<Vector2> results = new List<Vector2>();
        for (int i = 0; i < points.Length; i++)
        {
            if (grid[i] > 0)
            {
                results.Add(points[i]);
            }
        }

        grid.Dispose();
        points.Dispose();
        spawnPoints.Dispose();

        return results;
    }
}
