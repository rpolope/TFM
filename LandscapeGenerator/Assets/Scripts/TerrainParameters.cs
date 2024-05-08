using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public struct TerrainParameters
{
    [Header("Heightmap")]
    public NoiseParameters noiseParameters;

    [Header("Mesh")]
    public MeshParameters meshParameters;
}

[System.Serializable]
public struct NoiseParameters
{   
    public NoiseType noiseType;
    
    [SerializeField, Min(1)]
    public uint seed;
    
    [SerializeField, Min(0.001f)]
    public float scale;
    
    [SerializeField]
    public Vector2 offset;
    
    [SerializeField, Min(0.001f)]
    public float amplitude;
    
    [SerializeField, Range(0, 8)]
    public int octaves;
    
    [SerializeField]
    public float frequency;
    
    [SerializeField, Range(0, 1)]
    public float persistence;

    [SerializeField, Min(0.001f)] 
    public float lacunarity;
    
    // [SerializeField, Range(0, 1)]
    // public float ridgeRoughness;
    //
    [SerializeField, Range(0.1f, 10f)]
    public float ridgeRoughness;
}

[System.Serializable]
public struct MeshParameters
{
    public int resolution;
    
    public float heightScale;
    [Range(0,1)]
    public float waterLevel;
    [Range(0,6)]
    public int editorPreviewLOD;
}