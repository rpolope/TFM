using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
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
    public static Biome[] Biomes;
    private static Texture2D[] _colorMaps;
    private static readonly string[] LookupMapNames = { "biome-lookup-discrete", "biome-lookup-smooth" };

    public static void Initialize()
    {
        var parameters = new BiomesParameters(LookupMapNames);
        _colorMaps = parameters.colorMaps;
        Biomes = new Biome[parameters.climates.Length];
        for(int i = 0; i < Biomes.Length; i++)
        {
            Biomes[i] = GetBiomeFromClimate(parameters.climates[i]);
        }
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

    private static Biome GetBiomeFromClimate(Climate climate)
    {
        Biome biome = new Biome(0,0);
        biome.Climate = climate;
        
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
            default:
                throw new ArgumentOutOfRangeException(nameof(climate), climate, null);
        }

        return biome;
    }

    public static Color GetColorFromBiome(Biome biome)
    {
        int2 dimension = new int2(_colorMaps[0].width, _colorMaps[0].height);
        int x = (int)(biome.Moisture * dimension.x);
        int y = (int)(biome.Elevation * dimension.y);
        
        return _colorMaps[0].GetPixel(x, y);
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