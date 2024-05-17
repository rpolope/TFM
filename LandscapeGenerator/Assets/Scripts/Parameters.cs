using TMPro;
using UnityEngine;

[System.Serializable]
public struct TerrainParameters
{
    [Header("Heightmap")]
    public NoiseParameters noiseParameters;

    [Header("Mesh")]
    public MeshParameters meshParameters;
    
}
[System.Serializable]
public struct BiomesParameters
{
    public Climate[] climates;
    public Texture2D[] colorMaps;
    
    public BiomesParameters(string[] textureNames)
    {
        climates = new[]
        {
            Climate.OCEAN,
            Climate.BEACH,
            Climate.TEMPERATE_DESERT,
            Climate.SHRUBLAND,
            Climate.GRASSLAND,
            Climate.TEMPERATE_RAIN_FOREST,
            Climate.SCORCHED,
            Climate.SNOW
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
    
    [SerializeField, Range(0f, 1f)]
    public float ridgeness;
    
    [SerializeField, Range(0.1f, 10f)]
    public float ridgeRoughness;
}

[System.Serializable]
public struct MeshParameters
{
    public int resolution;
    [Min(1)]
    public float heightScale;
    [Range(0,1)]
    public float waterLevel;
    [Range(0,6)]
    public int levelsOfDetail;
}