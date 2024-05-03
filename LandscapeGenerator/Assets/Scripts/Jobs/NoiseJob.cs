using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;
using UnityEditor;

[BurstCompile]
public struct NoiseJob : IJobParallelFor
{
    private NativeArray<float> _heights;
    private NoiseSettings _noiseSettings;
    private int _size;
    private Vector2 _coords;

    public NoiseJob(NativeArray<float> heights, int size, Vector2 coords, NoiseSettings noiseSettings)
    {
        _heights = heights;
        _size = size;
        _noiseSettings = noiseSettings;
        _coords = coords;
    }
    public void Execute(int index)
    {
        int x = index % _size;
        int y = index / _size;
        _heights[index] = GetNoiseValue(x, y);
    }

    private float GetNoiseValue(int x, int y)
    {
        float noiseValue = 0.0f;
        switch (_noiseSettings.NoiseType)
        {
            case NoiseType.Perlin:
                noiseValue = Noise.GetPerlinFractalValue(x, y, _coords, _noiseSettings);
                break;
            case NoiseType.Simplex:
                break;
            case NoiseType.Worley:
                break;
        }
        return noiseValue;
    }
}