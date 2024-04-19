using Unity.Collections;
using Unity.Jobs;

public enum NoiseType
{
    Perlin,
    Simplex,
    Worley
};
public class NoiseGenerator : IMapGenerator
{
    public NoiseType NoiseType;
    
    public float[] GenerateMapWithJobs(int size)
    {
        NativeArray<float> heights = new NativeArray<float>(size * size, Allocator.TempJob);

        var job = new NoiseJob(heights,size, LandscapeManager.Instance.settings.NoiseSettings);

        JobHandle handle = job.Schedule(size * size, 64);
        handle.Complete();

        float[] calcHeights = new float[size * size];
        heights.CopyTo(calcHeights);

        heights.Dispose();
        return calcHeights;
    }
    
    public float[] GenerateMap(int size)
    {
       return GenerateMapWithJobs(size);
        
        float[] heights = new float[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                heights[x + y * size] = GetNoiseValue(x, y);
            }
        }
        return heights;
    }
    
    private float GetNoiseValue(int x, int y)
    {
        float noiseValue = 0.0f;
        switch (NoiseType)
        {
            case NoiseType.Perlin:
                noiseValue = Noise.GetPerlinFractalValue(x, y, LandscapeManager.Instance.settings.NoiseSettings);
                break;
            case NoiseType.Simplex:
                break;
            case NoiseType.Worley:
                break;
        }
        return noiseValue;
    }
   
}