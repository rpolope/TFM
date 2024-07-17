using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using Random = System.Random;

public enum ClimateType
{
    Grassland,
    Forest,
    Ocean,
    TropicalForest,
    Beach,
    Desert,
    Scorched,
    Shrubland,
    Tundra,
    Snow,
    Bare,
    Taiga,
    GrasslandHot,
    TemperateDesert,
    DesertHot,
    GrasslandCold,
    DesertCold
}

public enum BiomeType
{
    Polar,
    Tundra,
    BorealForest,
    TemperateConiferousForest,
    TemperateSeasonalForest,
    TropicalSeasonalForestSavanna,
    TropicalRainforest,
    WoodlandShrubland,
    TemperateGrasslandColdDesert,
    SubtropicalDesert
}


struct BiomeInfo
{
    public ClimateType climate;
    public float minTemp;
    public float maxTemp;
    public float minMoist;
    public float maxMoist;

    public BiomeInfo(ClimateType climate, float minTemp, float maxTemp, float minMoist, float maxMoist)
    {
        this.climate = climate;
        this.minTemp = minTemp;
        this.maxTemp = maxTemp;
        this.minMoist = minMoist;
        this.maxMoist = maxMoist;
    }
}

struct WhittakerBiomeInfo
{
    public BiomeType BiomeType;
    public float minTemp;
    public float maxTemp;
    public float minMoist;
    public float maxMoist;

    public WhittakerBiomeInfo(BiomeType biomeType, float minTemp, float maxTemp, float minMoist, float maxMoist)
    {
        this.BiomeType = biomeType;
        this.minTemp = minTemp;
        this.maxTemp = maxTemp;
        this.minMoist = minMoist;
        this.maxMoist = maxMoist;
    }
}

public class BiomesManager : MonoBehaviour
{
    internal static readonly BiomeInfo[] BiomesData = new BiomeInfo[]
    {
        new (ClimateType.Tundra, -30.0f, -10.0f, 0.1f, 0.5f),
        new (ClimateType.Forest, 0.0f, 10.0f, 0.5f, 1.0f),
        new (ClimateType.TropicalForest, 10.0f, 30.0f, 0.33f, 1.0f),
        new (ClimateType.Scorched, -30.0f, -10.0f, 0.0f, 0.1f),
        new (ClimateType.Shrubland, -10.0f, 0.0f, 0.33f, 0.66f),
        new (ClimateType.Snow, -30.0f, -10.0f, 0.5f, 1.0f),
        new (ClimateType.Bare, -30.0f, -10.0f, 0.1f, 0.2f),
        new (ClimateType.Taiga, -10.0f, 0.0f, 0.66f, 1.0f),
        new (ClimateType.GrasslandCold, 0.0f, 10.0f, 0.16f, 0.5f),
        new (ClimateType.GrasslandHot, 10.0f, 30.0f, 0.16f, 0.33f),
        new (ClimateType.DesertCold, -10.0f, 0.0f, 0.0f, 0.33f),
        new (ClimateType.TemperateDesert, 0.0f, 10.0f, 0.0f, 0.16f),
        new (ClimateType.DesertHot, 10.0f, 30.0f, 0.0f, 0.16f)
    };
    
    internal static readonly WhittakerBiomeInfo[] WhittakerDiagramInfo = new WhittakerBiomeInfo[]
    {
        new (BiomeType.Polar, -30.0f, -10.0f, 0.0f, 135.0f / 450.0f), // Blanco
        new (BiomeType.Tundra, -10.0f, 0.0f, 0.0f, 100.0f / 450.0f), // Azul
        new (BiomeType.BorealForest, -10.0f, 5.0f, 50.0f / 450.0f, 200.0f / 450.0f), // Verde oscuro
        new (BiomeType.TemperateConiferousForest, 5.0f, 20.0f, 150.0f / 450.0f, 400.0f / 450.0f), // Verde
        new (BiomeType.TemperateSeasonalForest, 5.0f, 20.0f, 50.0f / 450.0f, 200.0f / 450.0f), // Verde mar
        new (BiomeType.TropicalSeasonalForestSavanna, 20.0f, 30.0f, 50.0f / 450.0f, 250.0f / 450.0f), // Verde oliva
        new (BiomeType.TropicalRainforest, 20.0f, 30.0f, 250.0f / 450.0f, 400.0f / 450.0f), // Verde oscuro
        new (BiomeType.WoodlandShrubland, 10.0f, 20.0f, 0.0f, 100.0f / 450.0f), // Caqui oscuro
        new (BiomeType.TemperateGrasslandColdDesert, -5.0f, 10.0f, 0.0f, 50.0f / 450.0f), // Arena
        new (BiomeType.SubtropicalDesert, 20.0f, 30.0f, 0.0f, 100.0f / 450.0f) // Bronceado
    };

    
    public List<BiomeAsset> biomesAssets;

    private static BiomesAssetsManager _assetsManager;
    private static Biome[,] _biomes;
    private static Texture2D _colorMapTexture;
    public static Color[][] ColorMap { get; private set; }
    private static bool _isInitialized = false;
    public void Initialize()
    {
        if (_isInitialized && !Application.isPlaying) return;
        
        LoadColorMapTexture();
        _assetsManager = new BiomesAssetsManager(biomesAssets);
        if (Application.isPlaying)
            InitializeBiomes();
        _isInitialized = true;
    }

    private static void LoadColorMapTexture()
    {
        // _colorMapTexture = Resources.Load<Texture2D>("Textures/biome-lookup-discrete");
        _colorMapTexture = Resources.Load<Texture2D>("Textures/biome-lookup-smooth");
        if (_colorMapTexture == null)
        {
            Debug.LogError("No se pudo cargar la textura biome-lookup-discrete.");
            return;
        }

        ColorMap = new Color[_colorMapTexture.height][];
        for (int y = 0; y < _colorMapTexture.height; y++)
        {
            ColorMap[y] = new Color[_colorMapTexture.width];
            for (int x = 0; x < _colorMapTexture.width; x++)
            {
                ColorMap[y][x] = _colorMapTexture.GetPixel(y, x);
            }
        }
    }

    private static void InitializeBiomes()
    {
        int mapWidth = LandscapeManager.MapWidth;
        int mapHeight = LandscapeManager.MapHeight;

        _biomes = new Biome[mapWidth, mapHeight];

        for (int i = 0; i < mapHeight; i++)
        {
            for (int j = 0; j < mapWidth; j++)
            {
                var coordinates = new int2(j, i);
                var moisture = LandscapeManager.GetMoisture(coordinates);
                var heat = LandscapeManager.GetHeat(coordinates);
                
                _biomes[j, i] = new Biome(heat, moisture);
            }
        }
    }

    private static void UpdateBiomes()
    {
        foreach (var biome in _biomes)
        {
            biome.Update();
        }
    }
    
    public static Biome GetBiome(int2 coordinates)
    {
        return _biomes[coordinates.x, coordinates.y];
    }

    public static GameObject Instantiate(GameObject biomeAsset, Vector3 position)
    {
        return Instantiate(biomeAsset, position, Quaternion.identity);
    }
}

public class Biome
{
    public TerrainParameters TerrainParameters { get; private set; }
    public float Moisture { get; private set; }
    public float Heat { get; private set; }
    public Color[] ColorGradient { get; private set; }
    public ClimateType ClimateType { get; set; }
    public List<BiomeAsset> Assets { get; set; }

    public Biome(float heat, float moisture)
    {
        Moisture = moisture;
        Heat = heat;
        ClimateType = GetClimateType(moisture, heat);
        ColorGradient = BiomesManager.ColorMap[Mathf.RoundToInt(Moisture * 127)];
        // TerrainParameters = new TerrainParameters(new NoiseParameters(GetNoiseType()), new MeshParameters(0.1f));
        TerrainParameters = new TerrainParameters(new NoiseParameters(NoiseType.Perlin), new MeshParameters(0.1f));
        Assets = BiomesAssetsManager.GetAssetsForBiome(ClimateType);
    }

    public void Update() { }

    private NoiseType GetNoiseType()
    {
        return ClimateType switch
        {
            ClimateType.Grassland or ClimateType.Forest or ClimateType.Ocean => NoiseType.Perlin,
            ClimateType.TropicalForest => NoiseType.Simplex,
            ClimateType.Beach or ClimateType.Desert or ClimateType.Scorched or ClimateType.Shrubland => NoiseType.Voronoi,
            ClimateType.Tundra or ClimateType.Snow or ClimateType.Bare or ClimateType.Taiga => NoiseType.Ridged,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }
    
    private const float MaxTemperature = 30.0f;
    private const float MinTemperature = -30.0f;

    public static ClimateType GetClimateType(float moisture, float heat)
    {
        var temperature = heat * (MaxTemperature - MinTemperature) + MinTemperature;

        foreach (var biome in BiomesManager.BiomesData)
        {
            if (temperature >= biome.minTemp && temperature <= biome.maxTemp &&
                moisture >= biome.minMoist && moisture <= biome.maxMoist)
            {
                return biome.climate;
            }
        }
        return ClimateType.Ocean;
    }
    
    public static BiomeType GetBiomeType(float moisture, float heat)
    {
        var temperature = heat * (MaxTemperature - MinTemperature) + MinTemperature;

        foreach (var biome in BiomesManager.WhittakerDiagramInfo)
        {
            if (temperature >= biome.minTemp && temperature <= biome.maxTemp &&
                moisture >= biome.minMoist && moisture <= biome.maxMoist)
            {
                return biome.BiomeType;
            }
        }

        return BiomeType.Polar;
    }

    public float GetWaterProbability()
    {
        return ClimateType switch
        {
            ClimateType.GrasslandCold or ClimateType.GrasslandHot or ClimateType.Forest => 0.5f,
            ClimateType.TropicalForest => 0.8f,
            ClimateType.Beach or ClimateType.DesertCold or ClimateType.TemperateDesert or ClimateType.DesertHot or ClimateType.Scorched or ClimateType.Shrubland => 0.1f,
            ClimateType.Tundra or ClimateType.Snow or ClimateType.Bare or ClimateType.Taiga => 0.4f,
            ClimateType.Ocean => 1.0f,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

}
