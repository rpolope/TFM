using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct NoiseJob : IJobParallelFor
{
    private NativeArray<float> _heights;
    private NoiseSettings _noiseSettings;
    private int _size;

    public NoiseJob(NativeArray<float> heights, int size, NoiseSettings noiseSettings)
    {
        _heights = heights;
        _size = size;
        _noiseSettings = noiseSettings;
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
                noiseValue = Noise.GetPerlinFractalValue(x, y, _noiseSettings);
                break;
            case NoiseType.Simplex:
                break;
            case NoiseType.Worley:
                break;
        }
        return noiseValue;
    }
}