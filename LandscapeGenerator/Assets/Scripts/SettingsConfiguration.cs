using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public struct SettingsConfiguration
{
    [Header("Heightmap Settings")]
    public NoiseSettings NoiseSettings;

    [Header("Mesh")]
    public MeshSettings meshSettings;
}
//
// [System.Serializable]
// public struct NoiseSettings {
//     public NoiseType noiseType;
//     public float noiseScale;
//     public int octaves;
//     [Range(0,1)]
//     public float persistance;
//     public float lacunarity;
//     public uint seed;
//     public Vector2 offset;
// }

[System.Serializable]
public struct NoiseSettings
{   
    [SerializeField]
    private NoiseType _noiseType;
    
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

    public NoiseType NoiseType { get => _noiseType; set => _noiseType = value; }
    public float Scale { get => _scale; set => _scale = value; }
    public float Amplitude { get => _amplitude; set => _amplitude = value; }
    public int Octaves { get => _octaves; set => _octaves = value; }
    public float Frequency { get => _frequency; set => _frequency = value; }
    public float Persistence { get => _persistence; set => _persistence = value; }
    public float Lacunarity { get => _lacunarity; set => _lacunarity = value; }

}

[System.Serializable]
public struct MeshSettings {
    public float meshHeightMultiplier;
    [Range(0,1)]
    public float waterLevel;
    [Range(0,6)]
    public int editorPreviewLOD;
}

