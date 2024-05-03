using UnityEngine;

public static class Noise
{
    public static float GetPerlinFractalValue(float x, float y, Vector2 center, NoiseSettings noiseSettings)
    {
        
        float noiseValue = 0f;
        float maxAmplitude = 0f;
        float amplitude = noiseSettings.Amplitude;
        float frequency = noiseSettings.Frequency;

        for (int i = 0; i < noiseSettings.Octaves; i++)
        {
            float perlinValue = Mathf.PerlinNoise((center.x + x) / noiseSettings.Scale * frequency, (center.y + y) / noiseSettings.Scale * frequency);
            
            noiseValue += (perlinValue * 2 - 1) * amplitude;

            maxAmplitude += amplitude;
            amplitude *= noiseSettings.Persistence;
            frequency *= noiseSettings.Lacunarity;
        }

        noiseValue /= maxAmplitude;
        return noiseValue;
    }
}