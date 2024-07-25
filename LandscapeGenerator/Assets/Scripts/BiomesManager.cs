using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public enum BiomeType
{
    Default,
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
    public BiomeType Biome;
    public float minTemp;
    public float maxTemp;
    public float minMoist;
    public float maxMoist;

    public BiomeInfo(BiomeType biome, float minTemp, float maxTemp, float minMoist, float maxMoist)
    {
        this.Biome = biome;
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
        new (BiomeType.Snow, -30.0f, -10.0f, 0.0f, 1f),
        new (BiomeType.Tundra, -10.0f, -5.0f, 0.222f, 0.500f),
        new (BiomeType.BorealForest, -10.0f, -5.0f, 0.444f, 0.583f),
        new (BiomeType.TemperateConiferousForest, -5.0f, 5.0f, 0.333f, 0.889f),
        new (BiomeType.TemperateSeasonalForest, -5.0f, 5.0f, 0.111f, 0.444f),
        new (BiomeType.TropicalSeasonalForestSavanna, 5.0f, 10.0f, 0.111f, 0.556f),
        new (BiomeType.TropicalRainforest, 5.0f, 10.0f, 0.556f, 0.889f),
        new (BiomeType.WoodlandShrubland, 0.0f, 5.0f, 0.0f, 0.222f),
        new (BiomeType.TemperateGrasslandColdDesert, -15.0f, -5.0f, 0.0f, 0.111f),
        new (BiomeType.SubtropicalDesert, 10.0f, 30.0f, 0.0f, 0.222f)
    };

    private static Dictionary<BiomeType, int> _biomesCount = new Dictionary<BiomeType, int>();
    
    public List<BiomeAsset> biomesAssets;

    private static BiomesAssetsManager _assetsManager;
    private static Biome[,] _biomes;

    public void Initialize()
    {
        _assetsManager = new BiomesAssetsManager(biomesAssets);
        InitializeBiomes();
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
                
                if (_biomesCount.ContainsKey(_biomes[j, i].BiomeType))
                {
                    _biomesCount[_biomes[j, i].BiomeType]++;
                }
                else
                {
                    _biomesCount[_biomes[j, i].BiomeType] = 1;
                }
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
    public BiomeType BiomeType { get; set; }
    public List<BiomeAsset> Assets { get; set; }
    
    private const float MaxTemperature = 30.0f;
    private const float MinTemperature = -30.0f;

    public Biome(float heat, float moisture)
    {
        Moisture = moisture;
        Heat = heat;
        BiomeType = GetClimateType(moisture, heat);
        // TerrainParameters = new TerrainParameters(new NoiseParameters(GetNoiseType()), new MeshParameters(0.1f));
        TerrainParameters = new TerrainParameters(new NoiseParameters(NoiseType.Perlin), new MeshParameters(0.1f));
        Assets = BiomesAssetsManager.GetAssetsForBiome(BiomeType);
    }

    public void Update() { }

    private NoiseType GetNoiseType()
    {
        return BiomeType switch
        {
            BiomeType.TropicalSeasonalForestSavanna or BiomeType.TemperateSeasonalForest or BiomeType.BorealForest => NoiseType.Perlin,
            BiomeType.TropicalRainforest => NoiseType.Simplex,
            BiomeType.TemperateGrasslandColdDesert or BiomeType.SubtropicalDesert or BiomeType.WoodlandShrubland => NoiseType.Voronoi,
            BiomeType.Tundra or BiomeType.Snow or BiomeType.Snow => NoiseType.Ridged,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    private static BiomeType GetClimateType(float moisture, float heat)
    {
        var temperature = heat * (MaxTemperature - MinTemperature) + MinTemperature;

        BiomeType? closestClimateType = null;
        float closestDistance = float.MaxValue;

        foreach (var biome in BiomesManager.BiomesData)
        {
            if (temperature >= biome.minTemp && temperature <= biome.maxTemp &&
                moisture >= biome.minMoist && moisture <= biome.maxMoist) 
            {
                return biome.Biome;
            }
        
            float tempDistance = Math.Min(Math.Abs(temperature - biome.minTemp), Math.Abs(temperature - biome.maxTemp));
            float moistDistance = Math.Min(Math.Abs(moisture - biome.minMoist), Math.Abs(moisture - biome.maxMoist));
            float distance = tempDistance + moistDistance;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestClimateType = biome.Biome;
            }
        }

        // If no exact match, return the closest climate type
        return closestClimateType ?? BiomeType.Default;
    }

}
