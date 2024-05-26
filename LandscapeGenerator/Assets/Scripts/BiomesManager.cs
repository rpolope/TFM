using System;
using System.Collections.Generic;
using UnityEngine;

public enum ClimateType
{
    Grassland,
    TemperateDeciduousForest,
    Ocean,
    TemperateRainForest,
    TropicalSeasonalForest,
    TropicalRainForest,
    Beach,
    SubtropicalDesert,
    TemperateDesert,
    Scorched,
    Shrubland,
    Tundra,
    Snow,
    Bare,
    Taiga
}

public static class BiomesManager
{
    private static Biome[,] _biomes;
    private static bool _initialized = false;
    
    private static Texture2D _colorMapTexture;
    public static Color[][] ColorMap { get; private set; }
    public static Texture2D TextureTest;

    public static void Initialize()
    {
        if (_initialized) return;
        
        LoadColorMapTexture();
        InitializeBiomes();
        _initialized = true;
    }

    private static void LoadColorMapTexture()
    {
        _colorMapTexture = Resources.Load<Texture2D>("Textures/biome-lookup-discrete");
        if (_colorMapTexture == null)
        {
            Debug.LogError("No se pudo cargar la textura biome-lookup-discrete.");
            return;
        }

        TextureTest = new Texture2D(_colorMapTexture.width, _colorMapTexture.height);
        ColorMap = new Color[_colorMapTexture.height][];
        for (int y = 0; y < _colorMapTexture.height; y++)
        {
            ColorMap[y] = new Color[_colorMapTexture.width];
            for (int x = 0; x < _colorMapTexture.width; x++)
            {
                ColorMap[y][x] = _colorMapTexture.GetPixel(x, y);
                TextureTest.SetPixel(x, y, ColorMap[y][x]);
            }
        }
        
        TextureTest.Apply();
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
                var coordinates = new Coordinates(j, i);
                var moisture = LandscapeManager.GetMoisture(coordinates);
                var heat = LandscapeManager.GetHeat(coordinates);
                
                _biomes[i, j] = new Biome(moisture, heat);
            }
        }

        Debug.Log("Biomes initialized.");
    }

    private static void UpdateBiomes()
    {
        foreach (var biome in _biomes)
        {
            biome.Update();
        }
    }

    public static Biome GetBiome(Coordinates coordinates)
    {
        return _biomes[coordinates.Longitude, coordinates.Latitude];
    }
}

public class Biome
{
    public TerrainParameters TerrainParameters { get; private set; }
    public float Moisture { get; private set; }
    public float Heat { get; private set; }
    public ClimateType ClimateType { get; private set; }
    public Color[] ColorGradient { get; private set; }

    public Biome(float moisture, float heat)
    {
        Moisture = moisture;
        Heat = heat;
        ClimateType = GetClimateType(moisture, heat);
        ColorGradient = BiomesManager.ColorMap[(int)(Moisture * 127)];
        TerrainParameters = new TerrainParameters(new NoiseParameters(GetNoiseType()), new MeshParameters(0.1f));
    }

    public void Update() { }

    private NoiseType GetNoiseType()
    {
        return ClimateType switch
        {
            ClimateType.Grassland or ClimateType.TemperateDeciduousForest or ClimateType.Ocean => NoiseType.Perlin,
            ClimateType.TemperateRainForest or ClimateType.TropicalSeasonalForest or ClimateType.TropicalRainForest => NoiseType.Simplex,
            ClimateType.Beach or ClimateType.SubtropicalDesert or ClimateType.TemperateDesert or ClimateType.Scorched or ClimateType.Shrubland => NoiseType.Voronoi,
            ClimateType.Tundra or ClimateType.Snow or ClimateType.Bare or ClimateType.Taiga => NoiseType.Ridged,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }
    
    private static ClimateType GetClimateType(float moisture, float heat)
    {
        return heat switch
        {
            > 0.9f => ClimateType.Ocean,
            > 0.88f => ClimateType.Beach,
            > 0.8f => moisture switch
            {
                < 0.16f => ClimateType.TemperateDesert,
                < 0.50f => ClimateType.Grassland,
                < 0.83f => ClimateType.TemperateDeciduousForest,
                _ => ClimateType.TemperateRainForest
            },
            > 0.6f => moisture switch
            {
                < 0.16f => ClimateType.SubtropicalDesert,
                < 0.33f => ClimateType.Grassland,
                < 0.66f => ClimateType.TropicalSeasonalForest,
                _ => ClimateType.TropicalRainForest
            },
            > 0.3f => moisture switch
            {
                < 0.33f => ClimateType.TemperateDesert,
                < 0.66f => ClimateType.Shrubland,
                _ => ClimateType.Taiga
            },
            _ => moisture switch
            {
                < 0.1f => ClimateType.Scorched,
                < 0.2f => ClimateType.Bare,
                < 0.5f => ClimateType.Tundra,
                _ => ClimateType.Snow
            }
        };
    }
}
