using UnityEngine;

public static class Noise
{
    public static float GetPerlinFractalValue(float x, float y, NoiseSettings noiseSettings)
    {
        
        float noiseValue = 0f;
        float maxAmplitude = 0f;
        float amplitude = noiseSettings.Amplitude;
        float frequency = noiseSettings.Frequency;

        for (int i = 0; i < noiseSettings.Octaves; i++)
        {
            float perlinValue = Mathf.PerlinNoise(x / noiseSettings.Scale * frequency, y / noiseSettings.Scale * frequency);
            noiseValue += perlinValue * amplitude;

            maxAmplitude += amplitude;
            amplitude *= noiseSettings.Persistence;
            frequency *= noiseSettings.Lacunarity;
        }

        noiseValue /= maxAmplitude;
        return noiseValue;
    }
}