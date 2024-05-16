using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class MapGenerator : MonoBehaviour
{
    private const int MapSize = 129;
    
    public bool autoUpdate = true;
    public DrawMode drawMode;
    public TerrainParameters mapParameters;
    [Header("Biomes Params")]
    public BiomesParameters biomeParameters;

    private static Climate[] _regions = new[]
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
    
    [BurstCompile]
    private struct GenerateMapJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] 
        public NativeArray<float> Map;
        public float2 Centre;
        public NoiseParameters Parameters;

        public void Execute(int threadIndex)
        {
            int x = threadIndex % MapSize;
            int y = threadIndex / MapSize;
            
            float2 pos = new float2(x, y) + Centre;

            float value = NoiseGenerator.GetNoiseValue(pos, Parameters);

            Map[threadIndex] = value;
        }
    }
    
    public MapData GenerateMapData() {

        float[] noiseMap = GenerateMap(MapSize, new float2(), mapParameters.noiseParameters);
        
        BiomeManager biomeManager = new BiomeManager(biomeParameters);
        Color[] colorMap = GenerateColorMap(noiseMap, biomeManager.Biomes);
	
        return new MapData (noiseMap, colorMap);
    }

    private static Color[] GenerateColorMap(float[] noiseMap, Biome[] biomes)
    {
        Color[] colorMap = new Color[MapSize * MapSize];
        for (int y = 0; y < MapSize; y++) {
            for (int x = 0; x < MapSize; x++)
            {
                float currentHeight = noiseMap [x + y * MapSize];
                
                foreach (var biome in biomes)
                {
                    if (currentHeight > biome.Elevation) continue;
                    colorMap [y * MapSize + x] = BiomeManager.GetColorFromBiome(biome);
                    break;
                }
            }
        }

        return colorMap;
    }

    public static float[] GenerateMap(int mapSize, float2 centre, NoiseParameters parameters)
    {
        NativeArray<float> map = new NativeArray<float>(mapSize * mapSize, Allocator.TempJob);
        
        var generateMapJob = new GenerateMapJob()
        {
            Map = map,
            Centre = centre,
            Parameters = parameters
        };
        generateMapJob.Schedule( mapSize* mapSize, 3000).Complete();

        float[] mapArray = new float[mapSize * mapSize];
        map.CopyTo(mapArray);
        map.Dispose();
        
        return mapArray;
    }
}
public struct MapData {
    public float[] HeightMap;
    public Color[] ColourMap;

    public MapData (float[] heightMap, Color[] colourMap)
    {
        HeightMap = heightMap;
        ColourMap = colourMap;
    }
}

