using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using Random = System.Random;

public enum ClimateType
{
    Snow,
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

public class BiomesManager : MonoBehaviour
{
    internal static readonly BiomeInfo[] BiomesData = new BiomeInfo[]
    {
        new (ClimateType.Tundra, -10.0f, -5.0f, 0.222f, 0.500f),
        new (ClimateType.BorealForest, -10.0f, -5.0f, 0.444f, 0.583f),
        new (ClimateType.TemperateConiferousForest, -5.0f, 5.0f, 0.333f, 0.889f),
        new (ClimateType.TemperateSeasonalForest, -5.0f, 5.0f, 0.111f, 0.444f),
        new (ClimateType.TropicalSeasonalForestSavanna, 5.0f, 10.0f, 0.111f, 0.556f),
        new (ClimateType.TropicalRainforest, 5.0f, 10.0f, 0.556f, 0.889f),
        new (ClimateType.WoodlandShrubland, 0.0f, 5.0f, 0.0f, 0.222f),
        new (ClimateType.TemperateGrasslandColdDesert, -15.0f, -5.0f, 0.0f, 0.111f),
        new (ClimateType.SubtropicalDesert, 10.0f, 30.0f, 0.0f, 0.222f)
    };
    
    public List<BiomeAsset> biomesAssets;

    private static BiomesAssetsManager _assetsManager;
    private static Biome[,] _biomes;
    private static bool _isInitialized = false;

    public void Initialize()
    {
        if (_isInitialized && !Application.isPlaying) return;
        
        _assetsManager = new BiomesAssetsManager(biomesAssets);
        if (Application.isPlaying)
            InitializeBiomes();
        _isInitialized = true;
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

    private void OnValidate()
    {
        biomesAssets?.RemoveAll(biomeAsset => biomeAsset == null || biomeAsset.gameObjects.Count == 0);
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
    
    
    private const float MaxTemperature = 30.0f;
    private const float MinTemperature = -30.0f;

    public Biome(float heat, float moisture)
    {
        Moisture = moisture;
        Heat = heat;
        ClimateType = GetClimateType(moisture, heat);
        // TerrainParameters = new TerrainParameters(new NoiseParameters(GetNoiseType()), new MeshParameters(0.1f));
        TerrainParameters = new TerrainParameters(new NoiseParameters(NoiseType.Perlin), new MeshParameters(0.1f));
        Assets = BiomesAssetsManager.GetAssetsForBiome(ClimateType);
    }

    public void Update() { }

    private NoiseType GetNoiseType()
    {
        return ClimateType switch
        {
            ClimateType.TropicalSeasonalForestSavanna or ClimateType.TemperateSeasonalForest or ClimateType.BorealForest => NoiseType.Perlin,
            ClimateType.TropicalRainforest => NoiseType.Simplex,
            ClimateType.TemperateGrasslandColdDesert or ClimateType.SubtropicalDesert or ClimateType.WoodlandShrubland => NoiseType.Voronoi,
            ClimateType.Tundra or ClimateType.Snow or ClimateType.Snow => NoiseType.Ridged,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    private static ClimateType GetClimateType(float moisture, float heat)
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
        return ClimateType.SubtropicalDesert;
    }
}
