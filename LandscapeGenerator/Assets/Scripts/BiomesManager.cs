using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BiomeType {
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
        _biomes = new Biome[Enum.GetValues(typeof(BiomeType)).Length];
    }

    public static BiomeType GetBiomeTypeFromClimate(Biome.Climate climate)
    {
        
        switch (climate.Elevation)
        {
            case < 0.1f:
                return BiomeType.OCEAN;
            case < 0.12f:
                return BiomeType.BEACH;
            case > 0.8f:
                return climate.Moisture switch
                {
                    < 0.1f => BiomeType.SCORCHED,
                    < 0.2f => BiomeType.BARE,
                    < 0.5f => BiomeType.TUNDRA,
                    _ => BiomeType.SNOW
                };
            case > 0.6f:
                return climate.Moisture switch
                {
                    < 0.33f => BiomeType.TEMPERATE_DESERT,
                    < 0.66f => BiomeType.SHRUBLAND,
                    _ => BiomeType.TAIGA
                };
            case > 0.3f:
                return climate.Moisture switch
                {
                    < 0.16f => BiomeType.TEMPERATE_DESERT,
                    < 0.50f => BiomeType.GRASSLAND,
                    < 0.83f => BiomeType.TEMPERATE_DECIDUOUS_FOREST,
                    _ => BiomeType.TEMPERATE_RAIN_FOREST
                };
            default:
                return climate.Moisture switch
                {
                    < 0.16f => BiomeType.SUBTROPICAL_DESERT,
                    < 0.33f => BiomeType.GRASSLAND,
                    < 0.66f => BiomeType.TROPICAL_SEASONAL_FOREST,
                    _ => BiomeType.TROPICAL_RAIN_FOREST
                };
        }
    }

    public static Color GetColorFromClimate(Biome.Climate climate)
    {
        return _colorMaps[0].GetPixel((int)climate.Elevation, (int)climate.Moisture);
    }
}

public class Biome
{
    public GameObject[] Props;
    public GameObject[] Vegetation;
    public Texture2D[] Textures;
    
    private BiomeType _type;
    private Climate _climate;
    private Color _color;

    public Biome(float elevation, float moisture)
    {
        _climate = new Climate(elevation, moisture);
        _type = BiomeManager.GetBiomeTypeFromClimate(_climate);
        _color = BiomeManager.GetColorFromClimate(_climate);
    }

    public class Climate
    {
        public readonly float Elevation, Moisture;

        public Climate(float elevation, float moisture)
        {
            Elevation = elevation;
            Moisture = moisture;
        }
        
    }
}