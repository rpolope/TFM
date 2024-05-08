using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public enum NoiseType
{
    Perlin,
    Simplex,
    Voronoi,
    Ridged
}

public static class NoiseGenerator
{
    public static float GetNoiseValue(float2 position, NoiseParameters parameters)
    {
        float maxPossibleHeight = 0;
        float noiseHeight = 0;
        float amplitude = parameters.amplitude;
        float frequency = parameters.frequency;

        var random = new Random(parameters.seed);
        
        if (parameters.scale <= 0)
        {
            parameters.scale = 0.0001f;
        }
        
        for (int i = 0; i < parameters.octaves; i++)
        {
            float offsetX = random.NextFloat(-100000, 100000) + parameters.offset.x;
            float offsetY = random.NextFloat(-100000, 100000) - parameters.offset.y;
            
            float sampleX = (position.x + offsetX) / parameters.scale * frequency;
            float sampleY = (position.y + offsetY) / parameters.scale * frequency;

            float sampledNoiseValue = SampleNoiseValue(new float2(sampleX, sampleY), parameters.noiseType);
            noiseHeight += sampledNoiseValue * amplitude;

            maxPossibleHeight += amplitude;
            amplitude *= parameters.persistence;
            frequency *= parameters.lacunarity;
        }

        float normalizedHeight = noiseHeight / (maxPossibleHeight / 0.9f);
        noiseHeight = Mathf.Clamp(normalizedHeight, 0, maxPossibleHeight);
        
        return noiseHeight;
    }
    
    public static float GetRidgeNoiseSample(float2 sample) {
        return 2 * (0.5f - Mathf.Abs(0.5f - Mathf.PerlinNoise(sample.x, sample.y)));
    }

    public static float GetOctavedRidgeNoise(float2 sample, NoiseParameters parameters)
    {

        float[] heights = new float[parameters.octaves];
        float ampl = parameters.amplitude;
        float freq = parameters.frequency;
        float accum = 1f, maxAmpl = 0.0f;
        
        for (int o = 0; o < parameters.octaves; o++)
        {
            heights[o] = ampl * GetRidgeNoiseSample(sample / parameters.scale * freq) * accum;
            accum += heights[o];
            maxAmpl += ampl;

            ampl *= parameters.persistence;
            freq *= parameters.lacunarity;
        }
 
        return accum / maxAmpl; 
    }

    private static float SampleNoiseValue(float2 sample, NoiseType type)
    {
        float noiseValue = 0.0f;
        switch (type)
        {
            case NoiseType.Perlin:
                noiseValue = Mathf.PerlinNoise(sample.x, sample.y);
                break;
            case NoiseType.Simplex:
                noiseValue = (noise.snoise(sample) + 1) * 0.5f;
                break;
            case NoiseType.Ridged:
                noiseValue = 0f;
                break;
            case NoiseType.Voronoi:
                float2 cellularResult = noise.cellular(sample);
                float distanceToClosest = math.sqrt(cellularResult.x * cellularResult.x + cellularResult.y * cellularResult.y);
                noiseValue = distanceToClosest / 1.45f - 0.1f;
                break;
        }

        return noiseValue;
    }

    
}
