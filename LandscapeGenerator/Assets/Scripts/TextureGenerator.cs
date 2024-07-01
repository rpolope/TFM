using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using System;

public static class TextureGenerator
{
    public static Texture2D TextureFromColorMap(Color[] colorMap, int size)
    {
        Texture2D texture = new Texture2D(size, size);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colorMap);
        texture.Apply();
        return texture;
    }

    public static Texture2D TextureFromHeightMap(float[] heightMap)
    {
        int size = (int)Math.Sqrt(heightMap.Length);
        NativeArray<float> heightMapNative = new NativeArray<float>(heightMap, Allocator.TempJob);
        NativeArray<Color> colorMapNative = new NativeArray<Color>(heightMap.Length, Allocator.TempJob);

        var job = new HeightMapToColorMapJob
        {
            heightMap = heightMapNative,
            colorMap = colorMapNative
        };

        JobHandle handle = job.Schedule(heightMap.Length, 64);
        handle.Complete();

        Color[] colorMap = colorMapNative.ToArray();

        heightMapNative.Dispose();
        colorMapNative.Dispose();

        return TextureFromColorMap(colorMap, size);
    }

    [BurstCompile]
    struct HeightMapToColorMapJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float> heightMap;
        public NativeArray<Color> colorMap;

        public void Execute(int index)
        {
            colorMap[index] = Color.Lerp(Color.black, Color.white, heightMap[index]);
        }
    }
}