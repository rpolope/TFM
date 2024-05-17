using System;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public bool autoUpdate = true;
    public DrawMode drawMode;
    public TerrainParameters mapParameters;
    [Header("Biomes Params")]
    public BiomesParameters biomeParameters;

    public static MapGenerator Instance;

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
        public int MapSize;
        public float2 Centre;
        public NoiseParameters Parameters;

        public void Execute(int threadIndex)
        {
            int x = threadIndex % MapSize;
            int y = threadIndex / MapSize;
            
            float2 pos = new float2(x, y) + Centre;

            // float value = NoiseGenerator.GetNoiseValue(pos, Parameters);
            float value = GenerateHeight(pos);
            
            Map[threadIndex] = value;
        }
        
        private float GenerateHeight(float2 samplePos)
        {
            float ridgedFactor = Parameters.ridgeness;
            // float noiseValue = NoiseGenerator.GetNoiseValue(samplePos, TerrainParameters.noiseParameters);
            
            float noiseValue = (1 - ridgedFactor) * NoiseGenerator.GetNoiseValue(samplePos, Parameters);
            noiseValue += ridgedFactor * NoiseGenerator.GetFractalRidgeNoise(samplePos, Parameters);
            
            if (ridgedFactor > 0)
                noiseValue = Mathf.Pow(noiseValue, Parameters.ridgeRoughness);

            return noiseValue;
        }
    }


    private void Awake()
    {
        if (Instance != null)
        {
            Instance = this;
        }
        else
        {
            Destroy(Instance);
        }
    }

    public static MapData GenerateMapData(int resolution, float2 centre, NoiseParameters parameters) {

        float[] noiseMap = GenerateNoiseMap(resolution, new float2(), parameters);
        
        Color[] colorMap = GenerateColorMap(noiseMap);
	
        return new MapData (noiseMap, colorMap);
    }

    private static Color[] GenerateColorMap(float[] noiseMap)
    {
        BiomeManager.Initialize();
        
        var mapSize = noiseMap.Length;
        Color[] colorMap = new Color[mapSize];
        for (int i = 0; i < mapSize; i++) {
            
            float currentHeight = noiseMap [i];
            
            foreach (var biome in BiomeManager.Biomes)
            {
                if (currentHeight > biome.Elevation) continue;
                colorMap [i] = BiomeManager.GetColorFromBiome(biome);
                break;
            }
        }

        return colorMap;
    }

    public static float[] GenerateNoiseMap(int mapSize, float2 centre, NoiseParameters parameters)
    {
        NativeArray<float> map = new NativeArray<float>(mapSize * mapSize, Allocator.TempJob);
        
        var generateMapJob = new GenerateMapJob()
        {
            Map = map,
            Centre = centre,
            MapSize = mapSize,
            Parameters = parameters
        };
        generateMapJob.Schedule( mapSize* mapSize, 3000).Complete();

        var mapArray = new float[mapSize* mapSize];
        map.CopyTo(mapArray);
        map.Dispose();
        
        return mapArray;
    }
}
public struct MapData {
    public NativeArray<float> HeightMap;
    public NativeArray<Color> ColorMap;

    public MapData (float[] heightMap, Color[] colorMap)
    {
        HeightMap = new NativeArray<float>(heightMap, Allocator.Persistent);
        ColorMap = new NativeArray<Color>(colorMap, Allocator.Persistent);;
    }

    public void Dispose()
    {
        HeightMap.Dispose();
        ColorMap.Dispose();
    }
}

