using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Climate {
    OCEAN,
    BEACH,
    SCORCHED,
    BARE,
    TUNDRA,
    SNOW,
    TEMPERATE_DESERT,
    SHRUBLAND,
    TAIGA,
    GRASSLAND,
    TEMPERATE_DECIDUOUS_FOREST,
    TEMPERATE_RAIN_FOREST,
    SUBTROPICAL_DESERT,
    TROPICAL_SEASONAL_FOREST,
    TROPICAL_RAIN_FOREST
}

public class BiomeManager
{
    private static Texture2D[] _colorMaps;

    private Biome[] _biomes;

    public BiomeManager(Texture2D[] colorMaps)
    {
        _colorMaps = colorMaps;
        _biomes = new Biome[Enum.GetValues(typeof(Climate)).Length];
    }

    public static Climate GetClimateFromBiome(Biome biome)
    {
        return biome.Elevation switch
        {
            < 0.1f => Climate.OCEAN,
            < 0.12f => Climate.BEACH,
            > 0.8f => biome.Moisture switch
            {
                < 0.1f => Climate.SCORCHED,
                < 0.2f => Climate.BARE,
                < 0.5f => Climate.TUNDRA,
                _ => Climate.SNOW
            },
            > 0.6f => biome.Moisture switch
            {
                < 0.33f => Climate.TEMPERATE_DESERT,
                < 0.66f => Climate.SHRUBLAND,
                _ => Climate.TAIGA
            },
            > 0.3f => biome.Moisture switch
            {
                < 0.16f => Climate.TEMPERATE_DESERT,
                < 0.50f => Climate.GRASSLAND,
                < 0.83f => Climate.TEMPERATE_DECIDUOUS_FOREST,
                _ => Climate.TEMPERATE_RAIN_FOREST
            },
            _ => biome.Moisture switch
            {
                < 0.16f => Climate.SUBTROPICAL_DESERT,
                < 0.33f => Climate.GRASSLAND,
                < 0.66f => Climate.TROPICAL_SEASONAL_FOREST,
                _ => Climate.TROPICAL_RAIN_FOREST
            }
        };
    }
    
    public static Biome GetBiomeFromClimate(Climate climate)
    {
        Biome biome = new Biome(0,0);
        
        switch (climate)
        {
            case Climate.OCEAN:
                biome.Elevation = UnityEngine.Random.Range(0f, 0.1f);
                biome.Moisture = UnityEngine.Random.Range(0.1f, 0.5f);
                biome.Color = GetColorFromBiome(biome);
                break;
            case Climate.BEACH:
                biome.Elevation = UnityEngine.Random.Range(0.1f, 0.12f);
                biome.Moisture = UnityEngine.Random.Range(0.1f, 0.5f);
                biome.Color = GetColorFromBiome(biome);
                break;
            case Climate.SCORCHED:
                biome.Elevation = UnityEngine.Random.Range(0.8f, 1f);
                biome.Moisture = UnityEngine.Random.Range(0f, 0.1f);
                biome.Color = GetColorFromBiome(biome);
                break;
            case Climate.BARE:
                biome.Elevation = UnityEngine.Random.Range(0.8f, 1f);
                biome.Moisture = UnityEngine.Random.Range(0.1f, 0.2f);
                biome.Color = GetColorFromBiome(biome);
                break;
            case Climate.TUNDRA:
                biome.Elevation = UnityEngine.Random.Range(0.8f, 1f);
                biome.Moisture = UnityEngine.Random.Range(0.2f, 0.5f);
                biome.Color = GetColorFromBiome(biome);
                break;
            case Climate.SNOW:
                biome.Elevation = UnityEngine.Random.Range(0.8f, 1f);
                biome.Moisture = UnityEngine.Random.Range(0.5f, 1f);
                biome.Color = GetColorFromBiome(biome);
                break;
            case Climate.TEMPERATE_DESERT:
                biome.Elevation = UnityEngine.Random.Range(0.12f, 0.3f);
                biome.Moisture = UnityEngine.Random.Range(0f, 0.1f);
                biome.Color = GetColorFromBiome(biome);
                break;
            case Climate.SHRUBLAND:
                biome.Elevation = UnityEngine.Random.Range(0.6f, 0.8f);
                biome.Moisture = UnityEngine.Random.Range(0.33f, 0.66f);
                biome.Color = GetColorFromBiome(biome);
                break;
            case Climate.TAIGA:
                biome.Elevation = UnityEngine.Random.Range(0.6f, 0.8f);
                biome.Moisture = UnityEngine.Random.Range(0.66f, 1f);
                biome.Color = GetColorFromBiome(biome);
                break;
            case Climate.GRASSLAND:
                biome.Elevation = UnityEngine.Random.Range(0.3f, 0.6f);
                biome.Moisture = UnityEngine.Random.Range(0.33f, 0.66f);
                biome.Color = GetColorFromBiome(biome);
                break;
            case Climate.TEMPERATE_DECIDUOUS_FOREST:
                biome.Elevation = UnityEngine.Random.Range(0.3f, 0.6f);
                biome.Moisture = UnityEngine.Random.Range(0.16f, 0.83f);
                biome.Color = GetColorFromBiome(biome);
                break;
            case Climate.TEMPERATE_RAIN_FOREST:
                biome.Elevation = UnityEngine.Random.Range(0.3f, 0.6f);
                biome.Moisture = UnityEngine.Random.Range(0.5f, 1f);
                biome.Color = GetColorFromBiome(biome);
                break;
            case Climate.SUBTROPICAL_DESERT:
                biome.Elevation = UnityEngine.Random.Range(0.12f, 0.3f);
                biome.Moisture = UnityEngine.Random.Range(0f, 0.16f);
                biome.Color = GetColorFromBiome(biome);
                break;
            case Climate.TROPICAL_SEASONAL_FOREST:
                biome.Elevation = UnityEngine.Random.Range(0f, 0.3f);
                biome.Moisture = UnityEngine.Random.Range(0.33f, 0.66f);
                biome.Color = GetColorFromBiome(biome);
                break;
            case Climate.TROPICAL_RAIN_FOREST:
                biome.Elevation = UnityEngine.Random.Range(0f, 0.3f);
                biome.Moisture = UnityEngine.Random.Range(0.66f, 1f);
                biome.Color = GetColorFromBiome(biome);
                break;
        }

        return biome;
    }

    public static Color GetColorFromBiome(Biome biome)
    {
        return _colorMaps[0].GetPixel((int)biome.Elevation, (int)biome.Moisture);
    }
}

public class Biome
{
    public GameObject[] Props;
    public GameObject[] Vegetation;
    public Texture2D[] Textures;
    public float Elevation;
    public float Moisture;
    public Color Color;
    public Climate Climate;

    public Biome(float elevation, float moisture)
    {
        Elevation = elevation;
        Moisture = moisture;
        Color = BiomeManager.GetColorFromBiome(this);
        Climate = BiomeManager.GetClimateFromBiome(this);
    }
}