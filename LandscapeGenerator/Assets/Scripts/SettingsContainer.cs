using UnityEngine;

[System.Serializable]
public struct NoiseProperties
{
    [SerializeField, Min(0.001f)]
    private float _scale;
    
    [SerializeField, Min(0.001f)]
    private float _amplitude;
    
    [SerializeField, Range(0, 8)]
    private int _octaves;
    
    [SerializeField]
    private float _frequency;
    
    [SerializeField, Range(0, 1)]
    private float _persistence;

    [SerializeField, Min(0.001f)] 
    private float _lacunarity;

    public float Scale { get => _scale; set => _scale = value; }
    public float Amplitude { get => _amplitude; set => _amplitude = value; }
    public int Octaves { get => _octaves; set => _octaves = value; }
    public float Frequency { get => _frequency; set => _frequency = value; }
    public float Persistence { get => _persistence; set => _persistence = value; }
    public float Lacunarity { get => _lacunarity; set => _lacunarity = value; }

}

public static class SettingsContainer
{
    private static readonly NoiseProperties _noiseProperties;
    public static NoiseProperties NoiseProperties
    {
        get => _noiseProperties;
    }
}