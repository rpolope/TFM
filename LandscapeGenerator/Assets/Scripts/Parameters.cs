using System;
using UnityEngine;

[Serializable]
public struct TerrainParameters
{
    [Header("Heightmap")]
    public NoiseParameters noiseParameters;

    [Header("Mesh")]
    public MeshParameters meshParameters;
    
    public TerrainParameters(NoiseParameters noiseParameters, MeshParameters meshParameters)
    {
        this.noiseParameters = noiseParameters;
        this.meshParameters = meshParameters;
    }
}


[Serializable]
public struct BiomesParameters
{
    public ClimateType[] climates;
    public Texture2D[] colorMaps;
    
    public BiomesParameters(string[] textureNames)
    {
        climates = new[]
        {
            ClimateType.Ocean,
            ClimateType.Beach,
            ClimateType.Desert,
            ClimateType.Shrubland,
            ClimateType.Grassland,
            ClimateType.Forest,
            ClimateType.TropicalForest,
            ClimateType.Scorched,
            ClimateType.Snow
        };
        
        colorMaps = new Texture2D[textureNames.Length];
        
        for (int i = 0; i < textureNames.Length; i++)
        {
            colorMaps[i] = Resources.Load<Texture2D>($"Textures/{textureNames[i]}");
            if (colorMaps[i] == null)
            {
                Debug.LogError($"Texture '{textureNames[i]}' not found in Resources/Textures");
            }
        }
    }
}

[Serializable]
public struct NoiseParameters
{   
    public NoiseType noiseType;
    
    [SerializeField, Min(1)]
    public uint seed;
    
    [SerializeField, Min(0.001f)]
    public float scale;
    
    [SerializeField]
    public Vector2 offset;
    
    [SerializeField, Range(0, 8)]
    public int octaves;
    
    [SerializeField]
    public float frequency;
    
    [SerializeField, Range(0, 1)]
    public float persistence;

    [SerializeField, Min(0.001f)] 
    public float lacunarity;
    
    [SerializeField, Range(0f, 1f)]
    public float ridgeness;
    
    [SerializeField, Range(0.1f, 10f)]
    public float ridgeRoughness;
    
    public NoiseParameters(NoiseType type)
    {
        noiseType = type;
        seed = 1; 
        scale = 5; 
        offset = Vector2.zero; 
        octaves = 4;
        frequency = 0.5f; 
        persistence = 0.5f; 
        lacunarity = 1f; 
        ridgeness = 0.2f; 
        ridgeRoughness = 0f;
    }
}

[Serializable]
public struct MeshParameters
{
    [Min(1.0f)]
    public float scale;
    public int resolution;
    [Min(1)]
    public float heightScale;
    [Range(0,1)]
    public float waterLevel;
    [Range(0,6)]
    public int levelsOfDetail;

    public MeshParameters(float waterLevel)
    {
        scale = 2.5f;
        resolution = 11;
        heightScale = 11;
        levelsOfDetail = 1;
        this.waterLevel = waterLevel;
    }
}