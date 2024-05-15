using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public static class MapGenerator
{
    [BurstCompile]
    public struct GenerateMapJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] 
        public NativeArray<float> Map;
        public int MapSize;
        public float2 Centre;

        public void Execute(int threadIndex)
        {
            int x = threadIndex % MapSize;
            int y = threadIndex / MapSize;
            
            float2 pos = new float2(x, y);

            float value = 0f;

            Map[threadIndex] = value;
        }
    }

    public static float[] GenerateMap(int mapSize, float2 centre)
    {
        NativeArray<float> map = new NativeArray<float>(mapSize * mapSize, Allocator.TempJob);
        
        var generateMapJob = new GenerateMapJob()
        {
            Map = map,
            MapSize = mapSize,
            Centre = centre
        };
        var jobHandle = generateMapJob.Schedule(mapSize * mapSize, 3000);

        float[] mapArray = new float[mapSize * mapSize];
        map.CopyTo(mapArray);
        map.Dispose();
        
        return mapArray;
    }
}
